using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int maxHP = 50;
    private int currentHP;

    [Header("Damage Popup")]
    [SerializeField] private GameObject damagePopupPrefab;
    [SerializeField] private Transform popupSpawnPoint;
    [SerializeField] private Vector3 popupSpawnOffset = new Vector3(0f, 1f, 0f);

    [Header("Knockback")]
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float knockbackDuration = 0.2f;

    private Rigidbody2D rb;
    private EnemyAI enemyAI;
    private float knockbackTimer;

    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;
    public bool IsInKnockback => knockbackTimer > 0f;

    public event Action<int, int> OnHealthChanged;

    private void Awake()
    {
        currentHP = maxHP;
        rb = GetComponent<Rigidbody2D>();
        enemyAI = GetComponent<EnemyAI>();
        OnHealthChanged?.Invoke(currentHP, maxHP);
    }

    private void FixedUpdate()
    {
        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.fixedDeltaTime;
        }
    }

    public void TakeDamage(int amount, Vector2 damageSourcePosition)
    {
        if (amount <= 0) return;
        currentHP = Mathf.Max(0, currentHP - amount);
        OnHealthChanged?.Invoke(currentHP, maxHP);

        SpawnDamagePopup(amount);
        ApplyKnockback(damageSourcePosition);

        if (currentHP == 0) Die();
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;
        currentHP = Mathf.Max(0, currentHP - amount);
        OnHealthChanged?.Invoke(currentHP, maxHP);

        SpawnDamagePopup(amount);

        if (currentHP == 0) Die();
    }

    private void ApplyKnockback(Vector2 damageSourcePosition)
    {
        if (!rb) return;

        Vector2 knockbackDir = ((Vector2)transform.position - damageSourcePosition).normalized;
        rb.linearVelocity = knockbackDir * knockbackForce;
        knockbackTimer = knockbackDuration;
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;
        currentHP = Mathf.Min(maxHP, currentHP + amount);
        OnHealthChanged?.Invoke(currentHP, maxHP);
    }

    private void SpawnDamagePopup(int damageAmount)
    {
        if (!damagePopupPrefab) return;

        Vector3 spawnPos = popupSpawnPoint ? popupSpawnPoint.position : transform.position + popupSpawnOffset;
        GameObject popup = Instantiate(damagePopupPrefab, spawnPos, Quaternion.identity);

        DamagePopup popupScript = popup.GetComponent<DamagePopup>();
        if (popupScript)
        {
            popupScript.Initialize(damageAmount, spawnPos);
        }
    }

    private void Die()
    {
        Destroy(gameObject);
    }
}