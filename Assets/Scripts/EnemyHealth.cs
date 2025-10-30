using UnityEngine;
using System;
using System.Collections;

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

    [Header("Hit Animation")]
    [SerializeField] private float hitAnimationDuration = 0.3f;

    [Header("Death")]
    [SerializeField] private float deathAnimationDuration = 1f; // Duration of death animation
    [SerializeField] private bool disableCollisionOnDeath = true;

    private Rigidbody2D rb;
    private EnemyAI enemyAI;
    private Animator anim;
    private Collider2D[] colliders;
    private float knockbackTimer;
    private bool isPlayingHitAnimation;
    private bool isDead = false;

    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;
    public bool IsInKnockback => knockbackTimer > 0f;
    public bool IsPlayingHitAnimation => isPlayingHitAnimation;
    public bool IsDead => isDead;

    public event Action<int, int> OnHealthChanged;
    public event Action OnDeath;

    private void Awake()
    {
        currentHP = maxHP;
        rb = GetComponent<Rigidbody2D>();
        enemyAI = GetComponent<EnemyAI>();
        anim = GetComponent<Animator>();
        colliders = GetComponents<Collider2D>();
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
        if (amount <= 0 || isDead) return;

        currentHP = Mathf.Max(0, currentHP - amount);
        OnHealthChanged?.Invoke(currentHP, maxHP);

        SpawnDamagePopup(amount);

        if (currentHP == 0)
        {
            Die();
        }
        else
        {
            ApplyKnockback(damageSourcePosition);
            PlayHitAnimation();
        }
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || isDead) return;

        currentHP = Mathf.Max(0, currentHP - amount);
        OnHealthChanged?.Invoke(currentHP, maxHP);

        SpawnDamagePopup(amount);

        if (currentHP == 0)
        {
            Die();
        }
        else
        {
            PlayHitAnimation();
        }
    }

    private void PlayHitAnimation()
    {
        if (!anim) return;

        anim.SetTrigger("hit");

        if (!isPlayingHitAnimation)
        {
            StartCoroutine(HitAnimationCoroutine());
        }
    }

    private IEnumerator HitAnimationCoroutine()
    {
        isPlayingHitAnimation = true;
        yield return new WaitForSeconds(hitAnimationDuration);
        isPlayingHitAnimation = false;
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
        if (amount <= 0 || isDead) return;

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
        if (isDead) return;

        isDead = true;

        // Invoke death event
        OnDeath?.Invoke();

        // Stop all movement
        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Static; // Make it static so it doesn't fall or move
        }

        // Disable collisions if specified
        if (disableCollisionOnDeath && colliders != null)
        {
            foreach (var col in colliders)
            {
                if (col) col.enabled = false;
            }
        }

        // Disable AI
        if (enemyAI)
        {
            enemyAI.enabled = false;
        }

        // Play death animation and destroy after it finishes
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        // Trigger death animation
        if (anim)
        {
            anim.SetTrigger("death");
        }

        // Wait for animation to complete
        yield return new WaitForSeconds(deathAnimationDuration);

        // Destroy the game object
        Destroy(gameObject);
    }
}