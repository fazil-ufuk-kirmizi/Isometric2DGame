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
    [SerializeField] private float stunDuration = 0.3f;

    [Header("Item Drops")]
    [SerializeField] private ItemDrop[] itemDrops;
    [SerializeField] private GameObject worldItemPrefab;
    [SerializeField] private float dropForce = 3f;
    [SerializeField] private float dropRadius = 0.5f;

    [Header("Death")]
    [SerializeField] private float deathDelayAfterHit = 0.3f;
    [SerializeField] private float deathAnimationDuration = 1f;
    [SerializeField] private bool disableCollisionOnDeath = true;

    private Rigidbody2D rb;
    private EnemyAI enemyAI;
    private Animator anim;
    private Collider2D[] colliders;
    private float knockbackTimer;
    private float hitReactionTimer;
    private float stunTimer;
    private bool isPlayingHitAnimation;
    private bool isDead = false;

    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;
    public bool IsInKnockback => knockbackTimer > 0f;
    public bool IsInHitReaction => hitReactionTimer > 0f;
    public bool IsStunned => stunTimer > 0f && hitReactionTimer <= 0f;
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
        if (isDead && rb && rb.bodyType == RigidbodyType2D.Static)
        {
            return;
        }

        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.fixedDeltaTime;
        }

        if (hitReactionTimer > 0f)
        {
            hitReactionTimer -= Time.fixedDeltaTime;

            if (hitReactionTimer <= 0f && stunTimer > 0f)
            {
                if (rb && rb.bodyType != RigidbodyType2D.Static)
                {
                    rb.linearVelocity = Vector2.zero;
                }
            }
        }

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

        ApplyKnockback(damageSourcePosition);
        PlayHitAnimation();
        StartHitReaction();

        if (currentHP == 0)
        {
            StartCoroutine(DieAfterHit());
        }
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || isDead) return;

        currentHP = Mathf.Max(0, currentHP - amount);
        OnHealthChanged?.Invoke(currentHP, maxHP);

        SpawnDamagePopup(amount);

        PlayHitAnimation();
        StartHitReaction();

        if (currentHP == 0)
        {
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
        float hitReactionDuration = Mathf.Max(knockbackDuration, hitAnimationDuration);
        hitReactionTimer = hitReactionDuration;
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

        foreach (var itemDrop in itemDrops)
        {
            if (itemDrop.item == null) continue;

            Vector2 randomOffset = Random.insideUnitCircle * dropRadius;
            Vector3 spawnPos = transform.position + new Vector3(randomOffset.x, randomOffset.y, 0f);

            GameObject worldItemObj = Instantiate(worldItemPrefab, spawnPos, Quaternion.identity);
            WorldItem worldItem = worldItemObj.GetComponent<WorldItem>();

            if (worldItem)
            {
                worldItem.Initialize(itemDrop.item, itemDrop.quantity);
            }

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
        isDead = true;

        if (enemyAI)
        {
            enemyAI.enabled = false;
        }

        yield return new WaitForSeconds(deathDelayAfterHit);

        Die();
    }

    private void Die()
    {
        OnDeath?.Invoke();

        // NOTIFY QUEST SYSTEM
        NPCController.NotifyEnemyKilled();

        DropItems();

        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Static;
        }

        if (disableCollisionOnDeath && colliders != null)
        {
            foreach (var col in colliders)
            {
                if (col) col.enabled = false;
            }
        }

        knockbackTimer = 0f;
        hitReactionTimer = 0f;
        stunTimer = 0f;

        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        if (anim)
        {
            anim.SetTrigger("death");
        }

        yield return new WaitForSeconds(deathAnimationDuration);

        Destroy(gameObject);
    }
}