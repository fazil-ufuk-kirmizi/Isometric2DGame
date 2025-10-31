using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class NPCController : MonoBehaviour
{
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
    [SerializeField] private InputActionReference interactAction; // Must be assigned in Inspector
    [SerializeField] private GameObject interactPrompt;

    [Header("UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField, Min(0f)] private float textSpeed = 0.05f;

    [Header("Visual")]
    [SerializeField] private bool facePlayer = true;
    [SerializeField] private SpriteRenderer spriteRenderer;

    // Runtime state
    private GameObject player;
    private PlayerMovement playerMovement;
    private bool playerInRange;
    private bool isDialogueActive;
    private int currentDialogueIndex;
    private Coroutine typingCoroutine;
    private bool inputGuard;

    // Input
    private InputAction anyKeyAction;

    private void Start()
    {
        // Locate player and optional movement script
        player = GameObject.FindGameObjectWithTag("Player");
        if (player) playerMovement = player.GetComponent<PlayerMovement>();

        // Optional sprite renderer fallback
        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();

        // Initial UI state
        if (dialoguePanel) dialoguePanel.SetActive(false);
        if (interactPrompt) interactPrompt.SetActive(false);
    }

    private void OnEnable()
    {
        // Subscribe & enable Interact action
        if (interactAction != null && interactAction.action != null)
        {
            interactAction.action.started += OnInteractStarted;   // More responsive on some devices
            interactAction.action.performed += OnInteractPressed; // Standard path
            interactAction.action.Enable();
        }
        else
        {
            Debug.LogError("Interact ActionReference is not assigned or action is null.");
        }

        // Create "any key" action (enabled only while dialogue is open)
        anyKeyAction = new InputAction("AnyKey", InputActionType.Button);
        anyKeyAction.AddBinding("<Keyboard>/anyKey");
        anyKeyAction.AddBinding("*/<Button>"); // Gamepad/Mouse buttons
        anyKeyAction.started += OnAnyKeyPressed;
        // Do NOT enable here; it will be enabled when dialogue starts
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
    }

    private void Update()
    {
        if (!player) return;

        // Range check
        float distance = Vector2.Distance(transform.position, player.transform.position);
        bool wasInRange = playerInRange;
        playerInRange = distance <= interactionRange;

        if (playerInRange != wasInRange)
        {
            if (playerInRange) OnPlayerEnterRange();
            else OnPlayerExitRange();
        }

        // Face player while idle
        if (playerInRange && facePlayer && spriteRenderer && !isDialogueActive)
            FacePlayer();

        // Fallback: start dialogue if event missed but key was pressed this frame
        if (!inputGuard &&
            playerInRange &&
            !isDialogueActive &&
            interactAction != null &&
            interactAction.action != null &&
            interactAction.action.WasPressedThisFrame())
        {
            StartDialogue();
            inputGuard = true; // Prevent same-frame continue
        }

        // Clear guard at end of frame
        if (inputGuard) inputGuard = false;
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

    private void OnInteractStarted(InputAction.CallbackContext ctx)
    {
        // Some devices send started earlier than performed
        if (!isDialogueActive && playerInRange)
        {
            StartDialogue();
            inputGuard = true;
        }
    }

    private void OnInteractPressed(InputAction.CallbackContext ctx)
    {
        if (inputGuard) return;

        if (playerInRange && !isDialogueActive)
        {
            StartDialogue();
            inputGuard = true;
        }
        else if (isDialogueActive)
        {
            ContinueDialogue();
        }
    }

    private void OnAnyKeyPressed(InputAction.CallbackContext ctx)
    {
        if (inputGuard) return;
        if (isDialogueActive) ContinueDialogue();
    }

    private void StartDialogue()
    {
        if (dialogueLines == null || dialogueLines.Count == 0)
        {
            Debug.LogError("No dialogue lines to show.");
            return;
        }

        isDialogueActive = true;

        // Disable player movement while talking
        if (playerMovement) playerMovement.enabled = false;

        // Hide prompt
        if (interactPrompt) interactPrompt.SetActive(false);

        // Show panel and set name
        if (dialoguePanel) dialoguePanel.SetActive(true);
        if (nameText) nameText.text = npcName;

        // Pick first (or random) line
        currentDialogueIndex = randomizeDialogue ? Random.Range(0, dialogueLines.Count) : 0;

        ShowCurrentLine();

        // Enable "any key" only during dialogue
        if (anyKeyAction != null && !anyKeyAction.enabled) anyKeyAction.Enable();

        // Release guard next frame to avoid double-advance
        StartCoroutine(ReleaseGuardNextFrame());
    }

    private IEnumerator ReleaseGuardNextFrame()
    {
        yield return null;
        inputGuard = false;
    }

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
        // Simple typewriter effect
        foreach (char c in text)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(textSpeed);
        }
        typingCoroutine = null;
    }

    private void ContinueDialogue()
    {
        // If typing, finish instantly
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
            dialogueText.text = dialogueLines[currentDialogueIndex];
            return;
        }

        // Advance line or end
        currentDialogueIndex++;
        if (currentDialogueIndex < dialogueLines.Count && cycleThroughDialogue)
            ShowCurrentLine();
        else
            EndDialogue();
    }

    private void EndDialogue()
    {
        isDialogueActive = false;
        currentDialogueIndex = 0;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        // Re-enable movement
        if (playerMovement) playerMovement.enabled = true;

        // Hide panel
        if (dialoguePanel) dialoguePanel.SetActive(false);

        // Disable "any key"
        if (anyKeyAction != null && anyKeyAction.enabled) anyKeyAction.Disable();

        // Restore prompt if still in range
        if (playerInRange && interactPrompt) interactPrompt.SetActive(true);
    }

    private void FacePlayer()
    {
        if (!player || !spriteRenderer) return;
        spriteRenderer.flipX = player.transform.position.x < transform.position.x;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
