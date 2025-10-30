using UnityEngine;
using System;
using System.Collections;
using Random = UnityEngine.Random;

[System.Serializable]
public class ItemDrop
{
    public ItemDataSO item;
    public int quantity = 1;
}

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

    [Header("Stun")]
    [SerializeField] private float stunDuration = 0.3f; // Time enemy is stunned AFTER hit reaction (knockback + animation)

    [Header("Item Drops")]
    [SerializeField] private ItemDrop[] itemDrops; // Items to drop when enemy dies
    [SerializeField] private GameObject worldItemPrefab; // Prefab with WorldItem component
    [SerializeField] private float dropForce = 3f; // Force to pop items out
    [SerializeField] private float dropRadius = 0.5f; // Spread radius for items

    [Header("Death")]
    [SerializeField] private float deathDelayAfterHit = 0.3f; // Delay between hit animation and death animation
    [SerializeField] private float deathAnimationDuration = 1f; // Duration of death animation
    [SerializeField] private bool disableCollisionOnDeath = true;

    private Rigidbody2D rb;
    private EnemyAI enemyAI;
    private Animator anim;
    private Collider2D[] colliders;
    private float knockbackTimer;
    private float hitReactionTimer; // Tracks the full hit reaction time (knockback + animation)
    private float stunTimer;
    private bool isPlayingHitAnimation;
    private bool isDead = false;

    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;
    public bool IsInKnockback => knockbackTimer > 0f;
    public bool IsInHitReaction => hitReactionTimer > 0f; // True during knockback + hit animation
    public bool IsStunned => stunTimer > 0f && hitReactionTimer <= 0f; // Only stunned AFTER hit reaction
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
        // Don't update timers if dead and body is static
        if (isDead && rb && rb.bodyType == RigidbodyType2D.Static)
        {
            return;
        }

        // Count down knockback timer
        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.fixedDeltaTime;
        }

        // Count down hit reaction timer (longest of knockback or animation)
        if (hitReactionTimer > 0f)
        {
            hitReactionTimer -= Time.fixedDeltaTime;

            // When hit reaction ends, stop movement and start stun
            if (hitReactionTimer <= 0f && stunTimer > 0f)
            {
                // Only set velocity if body is not static
                if (rb && rb.bodyType != RigidbodyType2D.Static)
                {
                    rb.linearVelocity = Vector2.zero;
                }
            }
        }

        // Count down stun timer (only after hit reaction ends)
        if (stunTimer > 0f && hitReactionTimer <= 0f)
        {
            stunTimer -= Time.fixedDeltaTime;
        }
    }

    public void TakeDamage(int amount, Vector2 damageSourcePosition)
    {
        if (amount <= 0 || isDead) return;

        currentHP = Mathf.Max(0, currentHP - amount);
        OnHealthChanged?.Invoke(currentHP, maxHP);

        SpawnDamagePopup(amount);

        // Apply knockback and hit animation simultaneously
        ApplyKnockback(damageSourcePosition);
        PlayHitAnimation();
        StartHitReaction();

        if (currentHP == 0)
        {
            // Start death sequence after hit animation plays
            StartCoroutine(DieAfterHit());
        }
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || isDead) return;

        currentHP = Mathf.Max(0, currentHP - amount);
        OnHealthChanged?.Invoke(currentHP, maxHP);

        SpawnDamagePopup(amount);

        // Play hit animation and start hit reaction
        PlayHitAnimation();
        StartHitReaction();

        if (currentHP == 0)
        {
            // Start death sequence after hit animation plays
            StartCoroutine(DieAfterHit());
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

    private void StartHitReaction()
    {
        // Hit reaction lasts for the longer of knockback or animation
        float hitReactionDuration = Mathf.Max(knockbackDuration, hitAnimationDuration);
        hitReactionTimer = hitReactionDuration;

        // Stun starts after hit reaction ends
        stunTimer = hitReactionDuration + stunDuration;
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

    private void DropItems()
    {
        if (itemDrops == null || itemDrops.Length == 0 || !worldItemPrefab)
        {
            return;
        }

        // Spawn each item in the world
        foreach (var itemDrop in itemDrops)
        {
            if (itemDrop.item == null) continue;

            // Random position around the enemy
            Vector2 randomOffset = Random.insideUnitCircle * dropRadius;
            Vector3 spawnPos = transform.position + new Vector3(randomOffset.x, randomOffset.y, 0f);

            // Create the world item
            GameObject worldItemObj = Instantiate(worldItemPrefab, spawnPos, Quaternion.identity);
            WorldItem worldItem = worldItemObj.GetComponent<WorldItem>();

            if (worldItem)
            {
                worldItem.Initialize(itemDrop.item, itemDrop.quantity);
            }

            // Add a small force to make it "pop out"
            Rigidbody2D itemRb = worldItemObj.GetComponent<Rigidbody2D>();
            if (itemRb)
            {
                Vector2 randomDirection = Random.insideUnitCircle.normalized;
                itemRb.AddForce(randomDirection * dropForce, ForceMode2D.Impulse);
            }

            Debug.Log($"Enemy dropped: {itemDrop.item.itemName} x{itemDrop.quantity}");
        }
    }

    private IEnumerator DieAfterHit()
    {
        // Mark as dead to prevent further damage
        isDead = true;

        // Disable AI immediately so enemy stops moving/attacking
        if (enemyAI)
        {
            enemyAI.enabled = false;
        }

        // Wait for hit animation to play
        yield return new WaitForSeconds(deathDelayAfterHit);

        // Now proceed with death sequence
        Die();
    }

    private void Die()
    {
        // Invoke death event
        OnDeath?.Invoke();

        // Drop items into the world
        DropItems();

        // Stop all movement and make static
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

        // Clear all timers since enemy is dead
        knockbackTimer = 0f;
        hitReactionTimer = 0f;
        stunTimer = 0f;

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