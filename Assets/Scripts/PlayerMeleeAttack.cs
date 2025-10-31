using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerMeleeAttack : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference attackAction;

    [Header("Attack Settings")]
    [SerializeField] private float range = 1.2f;
    [SerializeField] private float arc = 120f;
    [SerializeField] private int damage = 20;
    [SerializeField] private float cooldown = 0.35f;
    [SerializeField] private LayerMask enemyMask;

    [Header("Attack Timing")]
    [SerializeField] private float damageDelay = 0.2f;

    [Header("References")]
    [SerializeField] private Camera cam;
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private PlayerMovement playerMovement;

    private float lastAttackTime;
    private Animator anim;

    private void OnEnable()
    {
        if (attackAction && attackAction.action != null)
        {
            attackAction.action.performed += OnAttack;
            attackAction.action.Enable();
        }

        if (!anim) anim = GetComponent<Animator>();
    }

    private void OnDisable()
    {
        if (attackAction && attackAction.action != null)
        {
            attackAction.action.performed -= OnAttack;
            attackAction.action.Disable();
        }
    }

    private void Start()
    {
        if (!inventoryManager)
            inventoryManager = FindFirstObjectByType<InventoryManager>();

        if (!playerMovement)
            playerMovement = GetComponent<PlayerMovement>();
        if (!playerMovement)
            playerMovement = FindFirstObjectByType<PlayerMovement>();

        if (!cam)
            cam = Camera.main;
    }

    private void Update()
    {
        // Fare yedeði (Input System yoksa)
        if ((!attackAction || attackAction.action == null) && Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
                TryAttack();
        }
    }

    private void OnAttack(InputAction.CallbackContext _)
    {
        TryAttack();
    }

    private void TryAttack()
    {
        // Diyalog/hareket kilitliyse
        if (playerMovement && playerMovement.IsPaused)
            return;

        // Envanter açýksa
        if (inventoryManager != null && inventoryManager.IsInventoryOpen)
            return;

        // Cooldown
        if (Time.time - lastAttackTime < cooldown)
            return;

        lastAttackTime = Time.time;

        if (anim) anim.SetTrigger("Attack");
        StartCoroutine(ApplyDamageAfterDelay());
    }

    private IEnumerator ApplyDamageAfterDelay()
    {
        Vector2 origin = attackOrigin ? (Vector2)attackOrigin.position : (Vector2)transform.position;
        Vector2 aimDir = Vector2.right;

        if (cam && Mouse.current != null)
        {
            Vector3 mouseWorld = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            mouseWorld.z = 0f;
            aimDir = ((Vector2)mouseWorld - origin).normalized;
            if (aimDir.sqrMagnitude < 0.0001f) aimDir = Vector2.right;
        }

        yield return new WaitForSeconds(damageDelay);

        origin = attackOrigin ? (Vector2)attackOrigin.position : (Vector2)transform.position;

        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, range, enemyMask);
        for (int i = 0; i < hits.Length; i++)
        {
            var hit = hits[i];
            if (!hit) continue;

            Vector2 toTarget = (Vector2)hit.bounds.center - origin;
            float angle = Vector2.Angle(aimDir, toTarget);
            if (angle > arc * 0.5f) continue;

            var enemy = hit.GetComponentInParent<EnemyHealth>();
            if (enemy != null)
                enemy.TakeDamage(damage, origin);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 o = attackOrigin ? attackOrigin.position : transform.position;
        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.3f);
        Gizmos.DrawWireSphere(o, range);
    }
#endif
}
