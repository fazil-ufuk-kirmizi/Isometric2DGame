using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHP = 100;
    [SerializeField] private int currentHP = 100;

    [Header("UI")]
    [SerializeField] private Slider healthBar;           // Red, instant HP bar (main)
    [SerializeField] private RectTransform damageFill;   // Yellow, delayed HP bar (Simple/Sliced, pivot (0,0.5))

    [Header("Damage Bar Effect")]
    [SerializeField, Min(0f)] private float holdDuration = 0.25f; // Delay before the yellow bar starts shrinking
    [SerializeField, Min(1f)] private float shrinkSpeedHP = 20f;  // Yellow bar shrink speed (HP per second)

    private float delayedHP;   // Current HP value represented by the yellow bar
    private float baseWidth;   // Full width of the yellow bar when HP is 100%
    private float holdTimer;   // Timer to handle the shrink delay

    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;

    private void Awake()
    {
        // Initialize the red bar
        if (healthBar)
        {
            healthBar.minValue = 0;
            healthBar.maxValue = maxHP;
            healthBar.value = currentHP;
        }

        // Initialize the yellow bar
        if (damageFill)
        {
            // Ensure the bar is anchored and pivoted to the left side
            damageFill.pivot = new Vector2(0f, 0.5f);
            damageFill.anchoredPosition = new Vector2(0f, damageFill.anchoredPosition.y);

            baseWidth = damageFill.sizeDelta.x;
            delayedHP = currentHP;
            SetDamageFillWidth(delayedHP / maxHP);
        }
    }

    private void Update()
    {
        if (!healthBar || !damageFill) return;

        // Wait before the yellow bar starts shrinking
        if (holdTimer > 0f)
        {
            holdTimer -= Time.deltaTime;
            return;
        }

        float targetHP = healthBar.value;

        // The yellow bar only decreases (simulating damage delay)
        if (delayedHP > targetHP)
        {
            delayedHP = Mathf.MoveTowards(delayedHP, targetHP, shrinkSpeedHP * Time.deltaTime);
            SetDamageFillWidth(delayedHP / maxHP);
        }
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;

        currentHP = Mathf.Max(0, currentHP - amount);

        // The red bar updates instantly
        if (healthBar) healthBar.value = currentHP;

        // Start the delay before the yellow bar shrinks
        holdTimer = holdDuration;
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;

        currentHP = Mathf.Min(maxHP, currentHP + amount);
        if (healthBar) healthBar.value = currentHP;

        // On healing, both bars update instantly
        delayedHP = currentHP;
        SetDamageFillWidth(delayedHP / maxHP);

        holdTimer = 0f;
    }

    public void SetMaxHP(int newMax, bool fill = true)
    {
        maxHP = Mathf.Max(1, newMax);
        currentHP = fill ? maxHP : Mathf.Min(currentHP, maxHP);

        if (healthBar)
        {
            healthBar.maxValue = maxHP;
            healthBar.value = currentHP;
        }

        delayedHP = currentHP;
        SetDamageFillWidth(delayedHP / maxHP);
        holdTimer = 0f;
    }

    private void SetDamageFillWidth(float ratio)
    {
        ratio = Mathf.Clamp01(ratio);
        var size = damageFill.sizeDelta;
        size.x = baseWidth * ratio;
        damageFill.sizeDelta = size;

        // Keep the yellow bar anchored to the left side
        damageFill.anchoredPosition = new Vector2(0f, damageFill.anchoredPosition.y);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (maxHP < 1) maxHP = 1;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        if (healthBar)
        {
            healthBar.minValue = 0;
            healthBar.maxValue = maxHP;
            healthBar.value = currentHP;
        }
        if (damageFill)
        {
            if (baseWidth <= 0f) baseWidth = damageFill.sizeDelta.x;
            delayedHP = currentHP;
            SetDamageFillWidth(delayedHP / maxHP);
        }
    }
#endif
}
