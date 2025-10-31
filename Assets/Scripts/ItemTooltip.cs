using UnityEngine;
using TMPro;

public class ItemTooltip : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textComponent;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Animation (Optional)")]
    [SerializeField] private bool animate = true;
    [SerializeField] private float fadeSpeed = 5f;
    [SerializeField] private float scaleSpeed = 5f;
    [SerializeField] private float targetScale = 1f;

    private float currentAlpha = 0f;
    private float currentScale = 0.8f;

    private void Awake()
    {
        // Auto-find components if not assigned
        if (!textComponent)
        {
            textComponent = GetComponentInChildren<TextMeshProUGUI>();
        }

        if (!canvasGroup)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (!canvasGroup)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        // Start invisible
        if (canvasGroup)
        {
            canvasGroup.alpha = 0f;
        }
    }

    private void OnEnable()
    {
        if (animate)
        {
            currentAlpha = 0f;
            currentScale = 0.8f;
        }
        else
        {
            currentAlpha = 1f;
            currentScale = targetScale;
            if (canvasGroup) canvasGroup.alpha = 1f;
            transform.localScale = Vector3.one * targetScale;
        }
    }

    private void Update()
    {
        if (!animate) return;

        // Fade in
        if (currentAlpha < 1f)
        {
            currentAlpha = Mathf.MoveTowards(currentAlpha, 1f, fadeSpeed * Time.deltaTime);
            if (canvasGroup) canvasGroup.alpha = currentAlpha;
        }

        // Scale up
        if (currentScale < targetScale)
        {
            currentScale = Mathf.MoveTowards(currentScale, targetScale, scaleSpeed * Time.deltaTime);
            transform.localScale = Vector3.one * currentScale;
        }
    }

    public void SetText(string text)
    {
        if (textComponent)
        {
            textComponent.text = text;
        }
    }

    public void SetColor(Color color)
    {
        if (textComponent)
        {
            textComponent.color = color;
        }
    }
}