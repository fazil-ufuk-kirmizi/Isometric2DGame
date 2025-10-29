using UnityEngine;
using UnityEngine.InputSystem;

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

    [Header("References")]
    [SerializeField] private Camera cam;
    [SerializeField] private Transform attackOrigin;

    private float lastAttackTime;

    private void OnEnable()
    {
        if (attackAction && attackAction.action != null)
        {
            attackAction.action.performed += OnAttack;
            attackAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (attackAction && attackAction.action != null)
        {
            attackAction.action.performed -= OnAttack;
            attackAction.action.Disable();
        }
    }

    private void Update()
    {
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
        if (Time.time - lastAttackTime < cooldown) return;
        lastAttackTime = Time.time;

        Vector2 origin = attackOrigin ? (Vector2)attackOrigin.position : (Vector2)transform.position;
        Camera cameraUsed = cam ? cam : Camera.main;

        Vector2 aimDir = Vector2.right;
        if (cameraUsed && Mouse.current != null)
        {
            Vector3 mouseWorld = cameraUsed.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            mouseWorld.z = 0f;
            aimDir = ((Vector2)mouseWorld - origin).normalized;
            if (aimDir.sqrMagnitude < 0.0001f) aimDir = Vector2.right;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, range, enemyMask);

        foreach (var hit in hits)
        {
            if (!hit) continue;
            Vector2 toTarget = (Vector2)hit.bounds.center - origin;
            float angle = Vector2.Angle(aimDir, toTarget);
            if (angle > arc * 0.5f) continue;

            EnemyHealth enemy = hit.GetComponentInParent<EnemyHealth>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage, origin);
            }
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
