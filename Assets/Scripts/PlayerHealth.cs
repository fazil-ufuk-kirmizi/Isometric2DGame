using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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

    [Header("Healing Effect")]
    [SerializeField, Min(1f)] private float healSpeedHP = 15f;    // HP restored per second (visual speed)
    [SerializeField] private bool smoothHealVisual = true;        // Enable smooth healing animation

    [Header("Animation")]
    [SerializeField] private string deathAnimationName = "Death"; // Name of the death animation state

    private float delayedHP;   // Current HP value represented by the yellow bar
    private float baseWidth;   // Full width of the yellow bar when HP is 100%
    private float holdTimer;   // Timer to handle the shrink delay
    private Animator anim;     // Reference to the Animator component
    private bool isDead = false; // Track if player is dead

    // Gradual healing system
    private float visualHP;    // The HP value shown on the red bar (smoothly animates to currentHP)
    private Coroutine healCoroutine;

    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;
    public bool IsDead => isDead;

    private void Awake()
    {
        // Get the Animator component
        anim = GetComponent<Animator>();

        // Initialize the red bar
        if (healthBar)
        {
            healthBar.minValue = 0;
            healthBar.maxValue = maxHP;
            healthBar.value = currentHP;
        }

        // Initialize visual HP to match current HP
        visualHP = currentHP;

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

        // Smoothly animate the red bar towards current HP (for healing)
        if (smoothHealVisual && visualHP < currentHP)
        {
            visualHP = Mathf.MoveTowards(visualHP, currentHP, healSpeedHP * Time.deltaTime);
            healthBar.value = visualHP;
        }

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
        // When healing, yellow bar follows the visual HP smoothly
        else if (delayedHP < targetHP)
        {
            delayedHP = Mathf.MoveTowards(delayedHP, targetHP, healSpeedHP * Time.deltaTime);
            SetDamageFillWidth(delayedHP / maxHP);
        }
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || isDead) return;

        currentHP = Mathf.Max(0, currentHP - amount);

        // The red bar updates instantly for damage
        visualHP = currentHP;
        if (healthBar) healthBar.value = currentHP;

        if (currentHP == 0)
        {
            // Play death animation directly
            isDead = true;
            if (anim) anim.Play(deathAnimationName);

            Debug.Log("Player died!");
        }
        else
        {
            // Trigger the hit animation only if not dead
            if (anim) anim.SetTrigger("Hit");
        }

        // Start the delay before the yellow bar shrinks
        holdTimer = holdDuration;
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || isDead) return;

        int oldHP = currentHP;
        currentHP = Mathf.Min(maxHP, currentHP + amount);

        if (currentHP == oldHP)
        {
            // No healing occurred (already at max)
            return;
        }

        // For smooth healing, we gradually animate the visual HP
        // The visualHP will catch up to currentHP in Update()
        if (smoothHealVisual)
        {
            // visualHP will gradually increase in Update() towards currentHP
            // This creates the smooth healing effect
        }
        else
        {
            // Instant healing (old behavior)
            visualHP = currentHP;
            if (healthBar) healthBar.value = currentHP;
            delayedHP = currentHP;
            SetDamageFillWidth(delayedHP / maxHP);
        }

        holdTimer = 0f;
    }

    /// <summary>
    /// Alternative: Heal over time with coroutine (more control)
    /// </summary>
    public void HealOverTime(int amount, float duration)
    {
        if (amount <= 0 || isDead) return;

        // Stop any existing heal coroutine
        if (healCoroutine != null)
        {
            StopCoroutine(healCoroutine);
        }

        healCoroutine = StartCoroutine(HealCoroutine(amount, duration));
    }

    private IEnumerator HealCoroutine(int amount, float duration)
    {
        int startHP = currentHP;
        int targetHP = Mathf.Min(maxHP, currentHP + amount);
        float elapsed = 0f;

        while (elapsed < duration && currentHP < targetHP)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Gradually increase HP
            int newHP = Mathf.RoundToInt(Mathf.Lerp(startHP, targetHP, t));
            currentHP = newHP;
            visualHP = newHP;

            if (healthBar) healthBar.value = visualHP;

            // Yellow bar follows
            delayedHP = visualHP;
            SetDamageFillWidth(delayedHP / maxHP);

            yield return null;
        }

        // Ensure we reach the target
        currentHP = targetHP;
        visualHP = targetHP;
        if (healthBar) healthBar.value = targetHP;
        delayedHP = targetHP;
        SetDamageFillWidth(delayedHP / maxHP);

        healCoroutine = null;
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

        visualHP = currentHP;
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