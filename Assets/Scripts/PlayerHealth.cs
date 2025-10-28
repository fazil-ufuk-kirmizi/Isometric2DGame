using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHP = 100;
    [SerializeField] private int currentHP = 100;

    [Header("UI")]
    [SerializeField] private Slider healthBar;           // red, instant
    [SerializeField] private RectTransform damageFill;   // yellow, delayed (Simple/Sliced, pivot (0,0.5))

    [Header("Damage Bar Effect")]
    [SerializeField, Min(0f)] private float holdDuration = 0.25f; // delay before yellow starts shrinking
    [SerializeField, Min(1f)] private float shrinkSpeedHP = 20f;  // yellow shrink speed in HP per second

    private float delayedHP;   // what yellow currently shows (in HP)
    private float baseWidth;   // full width of damageFill
    private float holdTimer;   // countdown

    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;

    private void Awake()
    {
        // init red bar
        if (healthBar)
        {
            healthBar.minValue = 0;
            healthBar.maxValue = maxHP;
            healthBar.value = currentHP;
        }

        // init yellow bar
        if (damageFill)
        {
            // Sol kenara sabitle (Inspector’da da pivot X=0, anchors min=max=(0,0.5) olmalý)
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

        // Bekleme süresi boyunca sarý barý kýpýrdatma
        if (holdTimer > 0f)
        {
            holdTimer -= Time.deltaTime;
            return;
        }

        // Hedef: kýrmýzý barýn (currentHP) gösterdiði deðer
        float targetHP = healthBar.value;

        // Sarý bar sadece AZALIR (damage için). Heal durumunda TakeDamage/Heal içinde ele alýyoruz.
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

        // Kýrmýzý bar hemen iner
        if (healthBar) healthBar.value = currentHP;

        // Gecikme baþlasýn; sarý bar bekleyip sonra akacak
        holdTimer = holdDuration;

        // Sarý barý YUKARI çekmeyiz; sadece Update'te aþaðý doðru akar
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;

        currentHP = Mathf.Min(maxHP, currentHP + amount);
        if (healthBar) healthBar.value = currentHP;

        // Heal'da ikisi de ayný anda yukarý çýksýn
        delayedHP = currentHP;
        SetDamageFillWidth(delayedHP / maxHP);

        // heal sonrasý bekleme olmasýn
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

        // solda sabit kalsýn
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
