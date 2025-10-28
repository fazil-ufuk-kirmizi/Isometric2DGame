using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Input System")]
    [Tooltip("Assign the 'Move' (Vector2) action from your Input Actions asset.")]
    [SerializeField] private InputActionReference moveAction;

    [Header("Movement")]
    [SerializeField] private float maxSpeed = 6f;          // Target top speed
    [SerializeField] private float acceleration = 40f;     // Rate when input is held
    [SerializeField] private float deceleration = 50f;     // Rate when input is released
    [SerializeField] private bool instantAcceleration = false; // If true, snap to target speed

    [Header("Visuals (Optional)")]
    [SerializeField] private SpriteRenderer spriteRenderer; // Horizontal flip only

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 targetVelocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (!spriteRenderer)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void OnEnable()
    {
        if (moveAction && moveAction.action != null)
            moveAction.action.Enable();
    }

    private void OnDisable()
    {
        if (moveAction && moveAction.action != null)
            moveAction.action.Disable();
    }

    private void Update()
    {
        // Read input as Vector2; normalize to avoid diagonal speed boost
        moveInput = moveAction ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
        if (moveInput.sqrMagnitude > 1f) moveInput = moveInput.normalized;

        // Compute desired velocity for FixedUpdate to chase
        targetVelocity = moveInput * maxSpeed;

        // Minimal visual feedback
        if (spriteRenderer && Mathf.Abs(moveInput.x) > 0.01f)
            spriteRenderer.flipX = moveInput.x < 0f;
    }

    private void FixedUpdate()
    {
        // Unity 6 API: use linearVelocity (velocity is obsolete)
        Vector2 current = rb.linearVelocity;

        if (instantAcceleration)
        {
            rb.linearVelocity = targetVelocity;
            return;
        }

        // Choose accel/decel based on input presence
        float rate = (moveInput.sqrMagnitude > 0.0001f) ? acceleration : deceleration;

        // Drive current velocity toward target velocity
        rb.linearVelocity = Vector2.MoveTowards(current, targetVelocity, rate * Time.fixedDeltaTime);
    }

    // Inspector helpers (kept minimal)
    public void SetMaxSpeed(float v) => maxSpeed = Mathf.Max(0f, v);
    public void SetAcceleration(float v) => acceleration = Mathf.Max(0f, v);
    public void SetDeceleration(float v) => deceleration = Mathf.Max(0f, v);
}
