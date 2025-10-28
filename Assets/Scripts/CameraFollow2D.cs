using UnityEngine;

[DefaultExecutionOrder(100)]
public class CameraFollow2D_DeadZone_NoSmooth : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Dead Zone (half extents)")]
    [SerializeField] private Vector2 deadZone = new Vector2(2f, 1.5f);

    [Header("Offset (Z locked)")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

    private Vector2 deadCenter; // world-space center of the dead zone

    private void OnEnable()
    {
        // Initialize dead zone center around target (or current camera position)
        if (target) deadCenter = target.position;
        else deadCenter = (Vector2)(transform.position - offset);
    }

    private void LateUpdate()
    {
        if (!target) return;

        Vector2 tpos = target.position;
        Vector2 delta = tpos - deadCenter;

        // Move dead zone center only when target exits thresholds (X)
        if (Mathf.Abs(delta.x) > deadZone.x)
            deadCenter.x += delta.x - Mathf.Sign(delta.x) * deadZone.x;

        // Move dead zone center only when target exits thresholds (Y)
        if (Mathf.Abs(delta.y) > deadZone.y)
            deadCenter.y += delta.y - Mathf.Sign(delta.y) * deadZone.y;

        // Place camera at (deadCenter + offset), with fixed Z from offset
        Vector3 desired = new Vector3(deadCenter.x + offset.x, deadCenter.y + offset.y, offset.z);
        transform.position = desired;
    }

    // --- Utilities ---

    /// <summary>Assign a new target and recenter the dead zone immediately.</summary>
    public void SetTarget(Transform newTarget, bool recenter = true)
    {
        target = newTarget;
        if (recenter && target)
            deadCenter = target.position;
    }

    /// <summary>Instantly align the camera and dead zone to the current target.</summary>
    public void Recenter()
    {
        if (!target) return;
        deadCenter = target.position;
        transform.position = new Vector3(deadCenter.x + offset.x, deadCenter.y + offset.y, offset.z);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!target) return;

        Gizmos.color = new Color(0f, 1f, 1f, 0.25f);

        // In edit mode, show the dead zone around the target; in play mode, show the runtime center
        Vector3 center = Application.isPlaying
            ? new Vector3(deadCenter.x + offset.x, deadCenter.y + offset.y, offset.z)
            : target.position + new Vector3(offset.x, offset.y, 0f);

        Gizmos.DrawWireCube(center, new Vector3(deadZone.x * 2f, deadZone.y * 2f, 0f));
    }
#endif
}
