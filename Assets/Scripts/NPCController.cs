using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using TMPro;

[System.Serializable]
public class Quest
{
    public string questName;
    [TextArea] public string description;
    public int requiredKills = 3;
    public ItemDrop[] rewards;

    [Header("Dialogue")]
    [TextArea] public string offerDialogue = "I need your help! Kill 3 enemies!";
    [TextArea] public string activeDialogue = "Have you dealt with those enemies yet?";
    [TextArea] public string completeDialogue = "Excellent! Here's your reward!";
    [TextArea] public string finishedDialogue = "Thanks again!";
}

public class NPCController : MonoBehaviour
{
    private enum DialogueMode { None, Normal, Quest }

    [Header("Dialogue")]
    [SerializeField] private string npcName = "NPC";
    [SerializeField, TextArea(3, 5)]
    private List<string> dialogueLines = new()
    {
        "Hello there, traveler!",
        "Nice weather we're having today.",
        "Safe travels!"
    };
    [SerializeField] private bool randomizeDialogue = false;
    [SerializeField] private bool cycleThroughDialogue = true;

    [Header("Quest System")]
    [SerializeField] private Quest quest; // Assign in Inspector

    private int currentKills = 0;
    private bool questGiven = false;
    private bool questCompleted = false;
    private bool questRewarded = false;

    [Header("Quest Tracker UI (Aynı Script İçinde)")]
    [Tooltip("Görev paneli (ör. Canvas > QuestTrackerPanel)")]
    [SerializeField] private GameObject questTrackerPanel;
    [Tooltip("Görev başlığı (TMP_Text)")]
    [SerializeField] private TextMeshProUGUI questTitleText;
    [Tooltip("İlerleme/Completed metni (TMP_Text)")]
    [SerializeField] private TextMeshProUGUI questProgressText;

    [Header("Interaction")]
    [SerializeField, Min(0f)] private float interactionRange = 2.84f;
    [SerializeField] private InputActionReference interactAction;
    [SerializeField] private GameObject interactPrompt;

    [Header("Dialogue UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField, Min(0f)] private float textSpeed = 0.05f;

    [Header("Choice UI")]
    [SerializeField] private GameObject choicePanel;
    [SerializeField] private Button questButton;
    [SerializeField] private TextMeshProUGUI questButtonText; // Text component of quest button
    [SerializeField] private Button talkButton;
    [SerializeField] private Button leaveButton;

    [Header("Visual")]
    [SerializeField] private bool facePlayer = true;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Safety Windows")]
    [SerializeField, Min(0f)] private float inputWarmupDuration = 0.25f;
    [SerializeField, Min(0f)] private float enterRangeCooldown = 0.15f;
    [SerializeField, Min(0f)] private float choiceBlockDuration = 0.2f;

    private GameObject player;
    private PlayerMovement playerMovement;
    private bool playerInRange;
    private bool isDialogueActive;
    private bool inChoiceMenu;
    private int currentDialogueIndex;
    private Coroutine typingCoroutine;
    private bool inputGuard;

    private float canAcceptInputAt = 0f;
    private float choiceBlockUntil = -1f;

    private InputAction anyKeyAction;

    // Statik: düşman öldürünce tüm görev verenlere haber
    private static readonly List<NPCController> questGivers = new();

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (player) playerMovement = player.GetComponent<PlayerMovement>();
        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();

        if (dialoguePanel) dialoguePanel.SetActive(false);
        if (choicePanel) choicePanel.SetActive(false);
        if (interactPrompt) interactPrompt.SetActive(false);

        // Quest Tracker UI varsayılan gizli
        if (questTrackerPanel) questTrackerPanel.SetActive(false);
        else TryAutoWireQuestTracker(); // Inspector boşsa otomatik bulmayı dene

        // Auto-wire quest button text if not assigned
        if (!questButtonText && questButton)
        {
            questButtonText = questButton.GetComponentInChildren<TextMeshProUGUI>();
        }

        canAcceptInputAt = Time.unscaledTime + inputWarmupDuration;

        if (quest != null && !questGivers.Contains(this))
        {
            questGivers.Add(this);
        }
    }

    private void OnDestroy()
    {
        questGivers.Remove(this);
    }

    private void OnEnable()
    {
        if (interactAction != null && interactAction.action != null)
        {
            interactAction.action.started += OnInteractStarted;
            interactAction.action.performed += OnInteractPressed;
            interactAction.action.Enable();
        }

        anyKeyAction = new InputAction("AnyKey", InputActionType.Button);
        anyKeyAction.AddBinding("<Keyboard>/anyKey");
        anyKeyAction.AddBinding("*/<Button>");
        anyKeyAction.started += OnAnyKeyPressed;

        if (questButton) questButton.onClick.AddListener(OnQuestSelected);
        if (talkButton) talkButton.onClick.AddListener(OnTalkSelected);
        if (leaveButton) leaveButton.onClick.AddListener(OnLeaveSelected);

        DisableButtonNavigation(questButton);
        DisableButtonNavigation(talkButton);
        DisableButtonNavigation(leaveButton);
        EventSystem.current?.SetSelectedGameObject(null);
    }

    private void OnDisable()
    {
        if (interactAction != null && interactAction.action != null)
        {
            interactAction.action.started -= OnInteractStarted;
            interactAction.action.performed -= OnInteractPressed;
            interactAction.action.Disable();
        }

        if (anyKeyAction != null)
        {
            anyKeyAction.started -= OnAnyKeyPressed;
            anyKeyAction.Disable();
            anyKeyAction.Dispose();
            anyKeyAction = null;
        }

        if (questButton) questButton.onClick.RemoveListener(OnQuestSelected);
        if (talkButton) talkButton.onClick.RemoveListener(OnTalkSelected);
        if (leaveButton) leaveButton.onClick.RemoveListener(OnLeaveSelected);
    }

    private void Update()
    {
        if (!player) return;

        float distance = Vector2.Distance(transform.position, player.transform.position);
        bool wasInRange = playerInRange;
        playerInRange = distance <= interactionRange;

        if (playerInRange != wasInRange)
        {
            if (playerInRange)
            {
                OnPlayerEnterRange();
                canAcceptInputAt = Mathf.Max(canAcceptInputAt, Time.unscaledTime + enterRangeCooldown);
            }
            else
            {
                OnPlayerExitRange();
            }
        }

        if (playerInRange && facePlayer && spriteRenderer && !isDialogueActive)
            FacePlayer();

        if (inputGuard) inputGuard = false;
    }

    private bool CanProcessInteract()
    {
        if (Time.unscaledTime < canAcceptInputAt) return false;
        if (inChoiceMenu) return false;
        return true;
    }

    private void StartDialogue()
    {
        // PlayerMovement.SetPaused mevcut değilse sorun çıkmasın diye null-check
        if (playerMovement && HasSetPaused(playerMovement)) playerMovement.SetPaused(true);

        isDialogueActive = true;
        inChoiceMenu = true;

        if (playerMovement) playerMovement.enabled = false;
        if (interactPrompt) interactPrompt.SetActive(false);

        if (dialoguePanel) dialoguePanel.SetActive(false);
        if (choicePanel) choicePanel.SetActive(true);

        // Update quest button text based on current state
        UpdateQuestButtonText();

        choiceBlockUntil = Time.unscaledTime + choiceBlockDuration;

        if (anyKeyAction != null && anyKeyAction.enabled) anyKeyAction.Disable();

        EventSystem.current?.SetSelectedGameObject(null);

        StartCoroutine(ReleaseGuardNextFrame());
    }

    private void UpdateQuestButtonText()
    {
        if (!questButtonText || quest == null) return;

        if (!questGiven)
        {
            questButtonText.text = "Accept Quest";
        }
        else if (questCompleted && !questRewarded)
        {
            questButtonText.text = "Turn In Quest";
        }
        else if (questRewarded)
        {
            questButtonText.text = "Quest";
        }
        else
        {
            questButtonText.text = "Quest Status";
        }
    }

    private void BeginLines(string customDialogue = null)
    {
        inChoiceMenu = false;
        if (choicePanel) choicePanel.SetActive(false);

        if (dialoguePanel) dialoguePanel.SetActive(true);
        if (nameText) nameText.text = npcName;

        if (!string.IsNullOrEmpty(customDialogue))
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            if (dialogueText)
            {
                dialogueText.text = string.Empty;
                typingCoroutine = StartCoroutine(TypeText(customDialogue));
            }
        }
        else
        {
            currentDialogueIndex = randomizeDialogue ? Random.Range(0, dialogueLines.Count) : 0;
            ShowCurrentLine();
        }

        if (anyKeyAction != null && !anyKeyAction.enabled) anyKeyAction.Enable();
    }

    private void OnQuestSelected()
    {
        if (ChoiceClicksBlocked()) return;

        if (quest == null)
        {
            BeginLines();
            return;
        }

        if (!questGiven)
        {
            // Accept Quest - Show offer dialogue
            questGiven = true;
            currentKills = 0;
            questCompleted = false;

            UpdateQuestTrackerUI();

            BeginLines(quest.offerDialogue);
            Debug.Log($"Quest started: {quest.questName}");
        }
        else if (questCompleted && !questRewarded)
        {
            // Turn in Quest - Show completion dialogue and give rewards
            GiveRewards();
            questRewarded = true;

            UpdateQuestTrackerUI(); // Show completed status

            BeginLines(quest.completeDialogue);
            StartCoroutine(HideTrackerAfterDelay(2f));
        }
        else if (questRewarded)
        {
            // Quest already finished
            BeginLines(quest.finishedDialogue);
        }
        else
        {
            // Quest in progress
            BeginLines(quest.activeDialogue);
        }
    }

    private IEnumerator HideTrackerAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (questTrackerPanel) questTrackerPanel.SetActive(false);
    }

    private void OnTalkSelected()
    {
        if (ChoiceClicksBlocked()) return;
        BeginLines();
    }

    private void OnLeaveSelected()
    {
        if (ChoiceClicksBlocked()) return;
        EndDialogue();
    }

    private bool ChoiceClicksBlocked() => Time.unscaledTime < choiceBlockUntil;

    private void ShowCurrentLine()
    {
        if (currentDialogueIndex >= dialogueLines.Count)
        {
            EndDialogue();
            return;
        }

        string line = dialogueLines[currentDialogueIndex];

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);

        if (dialogueText)
        {
            dialogueText.text = string.Empty;
            typingCoroutine = StartCoroutine(TypeText(line));
        }
    }

    private IEnumerator TypeText(string text)
    {
        foreach (char c in text)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(textSpeed);
        }
        typingCoroutine = null;
    }

    private void ContinueDialogue()
    {
        if (inChoiceMenu) return;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
            if (dialogueText && currentDialogueIndex < dialogueLines.Count)
                dialogueText.text = dialogueLines[currentDialogueIndex];
            return;
        }

        currentDialogueIndex++;
        if (currentDialogueIndex < dialogueLines.Count && cycleThroughDialogue)
            ShowCurrentLine();
        else
            EndDialogue();
    }

    private void EndDialogue()
    {
        isDialogueActive = false;
        inChoiceMenu = false;
        currentDialogueIndex = 0;

        if (playerMovement && HasSetPaused(playerMovement)) playerMovement.SetPaused(false);

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (playerMovement) playerMovement.enabled = true;

        if (dialoguePanel) dialoguePanel.SetActive(false);
        if (choicePanel) choicePanel.SetActive(false);

        if (anyKeyAction != null && anyKeyAction.enabled) anyKeyAction.Disable();

        if (playerInRange && interactPrompt) interactPrompt.SetActive(true);
    }

    // ===== QUEST TRACKER (aynı script) =====
    private void UpdateQuestTrackerUI()
    {
        if (quest == null) return;

        // Panel set
        if (!questTrackerPanel) TryAutoWireQuestTracker();

        if (questTrackerPanel) questTrackerPanel.SetActive(true);
        if (questTitleText) questTitleText.text = quest.questName;

        if (questProgressText)
        {
            if (questCompleted)
            {
                questProgressText.text = "COMPLETED! Return to quest giver.";
                questProgressText.color = Color.green;
            }
            else
            {
                questProgressText.text = $"Kill {quest.requiredKills} Enemies\nProgress: {currentKills}/{quest.requiredKills}";
                questProgressText.color = Color.white;
            }
        }
    }

    // Inspector boşsa isimle otomatik bağlama (opsiyonel)
    private void TryAutoWireQuestTracker()
    {
        if (!questTrackerPanel)
        {
            var panelObj = GameObject.Find("QuestTrackerPanel");
            if (panelObj) questTrackerPanel = panelObj;
        }
        if (!questTitleText && questTrackerPanel)
        {
            var t = questTrackerPanel.transform.Find("TitleText");
            if (t) questTitleText = t.GetComponent<TextMeshProUGUI>();
        }
        if (!questProgressText && questTrackerPanel)
        {
            var t = questTrackerPanel.transform.Find("ProgressText");
            if (t) questProgressText = t.GetComponent<TextMeshProUGUI>();
        }
    }

    public static void NotifyEnemyKilled()
    {
        foreach (var npc in questGivers)
        {
            if (npc.questGiven && !npc.questCompleted)
            {
                npc.OnEnemyKilled();
            }
        }
    }

    private void OnEnemyKilled()
    {
        if (quest == null || !questGiven || questCompleted) return;

        currentKills++;
        if (currentKills >= quest.requiredKills)
        {
            questCompleted = true;
            Debug.Log($"Quest completed: {quest.questName}");
        }

        UpdateQuestTrackerUI();
    }

    private void GiveRewards()
    {
        if (quest == null || quest.rewards == null) return;

        var inventory = FindFirstObjectByType<InventoryManager>();
        if (!inventory) return;

        foreach (var reward in quest.rewards)
        {
            if (reward.item == null) continue;

            Item newItem = new Item
            {
                itemName = reward.item.itemName,
                icon = reward.item.icon,
                quantity = reward.quantity,
                description = reward.item.description,
                itemData = reward.item
            };

            inventory.AddItem(newItem);
            Debug.Log($"Reward: {reward.item.itemName} x{reward.quantity}");
        }
    }

    // ===== INPUT/COLLATERAL =====
    private void OnInteractStarted(InputAction.CallbackContext ctx)
    {
        if (!playerInRange || isDialogueActive) return;
        if (!CanProcessInteract()) return;

        StartDialogue();
        inputGuard = true;
    }

    private void OnInteractPressed(InputAction.CallbackContext ctx)
    {
        if (inputGuard) return;

        if (!isDialogueActive)
        {
            if (!playerInRange) return;
            if (!CanProcessInteract()) return;

            StartDialogue();
            inputGuard = true;
        }
        else
        {
            if (inChoiceMenu) return;
            ContinueDialogue();
        }
    }

    private void OnAnyKeyPressed(InputAction.CallbackContext ctx)
    {
        if (inputGuard) return;
        if (isDialogueActive && !inChoiceMenu) ContinueDialogue();
    }

    private IEnumerator ReleaseGuardNextFrame()
    {
        yield return null;
        inputGuard = false;
    }

    private void OnPlayerEnterRange()
    {
        if (!isDialogueActive && interactPrompt) interactPrompt.SetActive(true);
    }

    private void OnPlayerExitRange()
    {
        if (interactPrompt) interactPrompt.SetActive(false);
        if (isDialogueActive) EndDialogue();
    }

    private void FacePlayer()
    {
        if (!player || !spriteRenderer) return;
        spriteRenderer.flipX = player.transform.position.x < transform.position.x;
    }

    private void DisableButtonNavigation(Button b)
    {
        if (!b) return;
        var nav = b.navigation;
        nav.mode = Navigation.Mode.None;
        b.navigation = nav;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }

    // PlayerMovement.SetPaused var mı kontrolü (yoksa çağırmayalım)
    private bool HasSetPaused(PlayerMovement pm)
    {
        return pm.GetType().GetMethod("SetPaused") != null;
    }
}