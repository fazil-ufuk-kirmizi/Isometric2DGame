using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Input System")]
    [Tooltip("Assign the 'Move' (Vector2) action from your Input Actions asset.")]
    [SerializeField] private InputActionReference moveAction;

    [Header("Movement")]
    [SerializeField] private float maxSpeed = 6f;
    [SerializeField] private float acceleration = 40f;
    [SerializeField] private float deceleration = 50f;
    [SerializeField] private bool instantAcceleration = false;

    [Header("Visuals (Optional)")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 targetVelocity;
    private Animator anim;

    // Pause flag
    private bool paused;

    // PUBLIC READ-ONLY ACCESSOR (fix for IsPaused error)
    public bool IsPaused => paused;

    private void Awake()
    {
        anim = GetComponent<Animator>();
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
        if (paused)
        {
            moveInput = Vector2.zero;
            targetVelocity = Vector2.zero;
            if (anim) anim.SetBool("isRunning", false);
            return;
        }

        moveInput = moveAction ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
        if (moveInput.sqrMagnitude > 1f) moveInput = moveInput.normalized;

        targetVelocity = moveInput * maxSpeed;

        if (spriteRenderer && Mathf.Abs(moveInput.x) > 0.01f)
            spriteRenderer.flipX = moveInput.x < 0f;

        bool isMoving = moveInput.sqrMagnitude > 0.01f;
        if (anim) anim.SetBool("isRunning", isMoving);
    }

    private void FixedUpdate()
    {
        if (paused)
        {
            rb.linearVelocity = Vector2.zero; // Unity 6
            return;
        }

        Vector2 current = rb.linearVelocity;

        if (instantAcceleration)
        {
            rb.linearVelocity = targetVelocity;
            return;
        }

        float rate = (moveInput.sqrMagnitude > 0.0001f) ? acceleration : deceleration;
        rb.linearVelocity = Vector2.MoveTowards(current, targetVelocity, rate * Time.fixedDeltaTime);
    }

    // Inspector helpers
    public void SetMaxSpeed(float v) => maxSpeed = Mathf.Max(0f, v);
    public void SetAcceleration(float v) => acceleration = Mathf.Max(0f, v);
    public void SetDeceleration(float v) => deceleration = Mathf.Max(0f, v);

    public void HardStop()
    {
        moveInput = Vector2.zero;
        targetVelocity = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
        if (anim) anim.SetBool("isRunning", false);
    }

    public void SetPaused(bool value)
    {
        paused = value;
        if (paused) HardStop();
    }
}
