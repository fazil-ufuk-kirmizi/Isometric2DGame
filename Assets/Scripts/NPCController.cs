using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using TMPro;

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

    [Header("Interaction")]
    [SerializeField, Min(0f)] private float interactionRange = 2.84f;
    [SerializeField] private InputActionReference interactAction; // assign in Inspector
    [SerializeField] private GameObject interactPrompt;

    [Header("Dialogue UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField, Min(0f)] private float textSpeed = 0.05f;

    [Header("Choice UI")]
    [SerializeField] private GameObject choicePanel; // panel with 3 buttons
    [SerializeField] private Button questButton;
    [SerializeField] private Button talkButton;
    [SerializeField] private Button leaveButton;

    [Header("Visual")]
    [SerializeField] private bool facePlayer = true;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Safety Windows")]
    [SerializeField, Min(0f)] private float inputWarmupDuration = 0.25f; // ignore inputs just after enable
    [SerializeField, Min(0f)] private float enterRangeCooldown = 0.15f;  // ignore inputs right after entering range
    [SerializeField, Min(0f)] private float choiceBlockDuration = 0.2f;  // ignore clicks just after opening menu

    // Runtime state
    private GameObject player;
    private PlayerMovement playerMovement;
    private bool playerInRange;
    private bool isDialogueActive;
    private bool inChoiceMenu;
    private int currentDialogueIndex;
    private Coroutine typingCoroutine;
    private bool inputGuard;

    // Timers
    private float canAcceptInputAt = 0f;  // global warmup + enter-range gate
    private float choiceBlockUntil = -1f; // choice menu click gate

    // Input
    private InputAction anyKeyAction;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (player) playerMovement = player.GetComponent<PlayerMovement>();
        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();

        if (dialoguePanel) dialoguePanel.SetActive(false);
        if (choicePanel) choicePanel.SetActive(false);
        if (interactPrompt) interactPrompt.SetActive(false);

        // Global warmup from start
        canAcceptInputAt = Time.unscaledTime + inputWarmupDuration;
    }

    private void OnEnable()
    {
        if (interactAction != null && interactAction.action != null)
        {
            interactAction.action.started += OnInteractStarted;
            interactAction.action.performed += OnInteractPressed;
            interactAction.action.Enable();
        }
        else
        {
            Debug.LogError("Interact ActionReference is not assigned or action is null.");
        }

        anyKeyAction = new InputAction("AnyKey", InputActionType.Button);
        anyKeyAction.AddBinding("<Keyboard>/anyKey");
        anyKeyAction.AddBinding("*/<Button>");
        anyKeyAction.started += OnAnyKeyPressed;
        // Not enabled here; enabled when lines begin

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
                // Gate inputs briefly after entering range
                canAcceptInputAt = Mathf.Max(canAcceptInputAt, Time.unscaledTime + enterRangeCooldown);
            }
            else
            {
                OnPlayerExitRange();
            }
        }

        if (playerInRange && facePlayer && spriteRenderer && !isDialogueActive)
            FacePlayer();

        // No WasPressedThisFrame fallback here (prevents accidental auto-open)
        if (inputGuard) inputGuard = false;
    }

    private bool CanProcessInteract()
    {
        // Global warmup, enter-range cooldown, menu state
        if (Time.unscaledTime < canAcceptInputAt) return false;
        if (inChoiceMenu) return false;
        return true;
    }

    // Open choice menu only (no dialogue panel yet)
    private void StartDialogue()
    {
        if (dialogueLines == null || dialogueLines.Count == 0)
        {
            Debug.LogError("No dialogue lines to show.");
            return;
        }

        if (playerMovement)
        {
            playerMovement.SetPaused(true);
        }

        isDialogueActive = true;
        inChoiceMenu = true;

        if (playerMovement) playerMovement.enabled = false;
        if (interactPrompt) interactPrompt.SetActive(false);

        // Only show choice panel, NOT dialogue panel
        if (dialoguePanel) dialoguePanel.SetActive(false);
        if (choicePanel) choicePanel.SetActive(true);

        // Block same-frame submits
        choiceBlockUntil = Time.unscaledTime + choiceBlockDuration;

        // Any-key disabled while choosing
        if (anyKeyAction != null && anyKeyAction.enabled) anyKeyAction.Disable();

        EventSystem.current?.SetSelectedGameObject(null);

        StartCoroutine(ReleaseGuardNextFrame());
    }

    private void BeginLines()
    {
        inChoiceMenu = false;
        if (choicePanel) choicePanel.SetActive(false);

        // NOW activate the dialogue panel for actual dialogue
        if (dialoguePanel) dialoguePanel.SetActive(true);
        if (nameText) nameText.text = npcName;

        currentDialogueIndex = randomizeDialogue ? Random.Range(0, dialogueLines.Count) : 0;
        ShowCurrentLine();

        if (anyKeyAction != null && !anyKeyAction.enabled) anyKeyAction.Enable();

    }

    private void OnQuestSelected()
    {
        if (ChoiceClicksBlocked()) return;
        BeginLines();
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
        else
        {
            Debug.LogError("Dialogue Text is not assigned.");
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

        if (playerMovement) playerMovement.SetPaused(false);

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

    // Input callbacks
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

    // Range/UI helpers
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
}