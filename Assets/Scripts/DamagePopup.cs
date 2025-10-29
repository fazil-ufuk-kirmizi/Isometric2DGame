using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private float lifetime = 1f;
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 0.2f, 1.2f);
    [SerializeField] private AnimationCurve fadeOutCurve = AnimationCurve.Linear(0.5f, 1f, 1f, 0f);

    [Header("Randomization")]
    [SerializeField] private Vector2 randomOffset = new Vector2(0.5f, 0.3f);

    private TextMeshProUGUI textMesh;
    private float timer;
    private Vector3 moveDirection;
    private Color originalColor;
    private Vector3 originalScale;

    private void Awake()
    {
        textMesh = GetComponentInChildren<TextMeshProUGUI>();
        if (textMesh) originalColor = textMesh.color;
        originalScale = transform.localScale;
    }

    public void Initialize(int damageAmount, Vector3 position)
    {
        if (textMesh)
        {
            textMesh.text = damageAmount.ToString();
            textMesh.color = originalColor;
        }

        // Add random offset to position
        float offsetX = Random.Range(-randomOffset.x, randomOffset.x);
        float offsetY = Random.Range(0f, randomOffset.y);
        transform.position = position + new Vector3(offsetX, offsetY, 0f);

        // Random upward direction
        moveDirection = new Vector3(Random.Range(-0.3f, 0.3f), 1f, 0f).normalized;

        timer = 0f;
        transform.localScale = Vector3.zero; // Start from zero scale
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float normalizedTime = timer / lifetime;

        // Pop-up scale effect
        float scaleValue = scaleCurve.Evaluate(normalizedTime);
        transform.localScale = originalScale * scaleValue;

        // Move upward
        transform.position += moveDirection * (moveSpeed * Time.deltaTime);

        // Fade out based on curve
        if (textMesh)
        {
            float alpha = fadeOutCurve.Evaluate(normalizedTime);
            Color c = originalColor;
            c.a = alpha;
            textMesh.color = c;
        }

        // Destroy when lifetime expires
        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}