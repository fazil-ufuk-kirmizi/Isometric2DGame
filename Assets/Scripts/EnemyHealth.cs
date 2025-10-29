using UnityEngine;
using System;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int maxHP = 50;
    private int currentHP;

    [Header("Damage Popup")]
    [SerializeField] private GameObject damagePopupPrefab;
    [SerializeField] private Transform popupSpawnPoint; // Optional: specific spawn location
    [SerializeField] private Vector3 popupSpawnOffset = new Vector3(0f, 1f, 0f);

    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;

    public event Action<int, int> OnHealthChanged;

    private void Awake()
    {
        currentHP = maxHP;
        OnHealthChanged?.Invoke(currentHP, maxHP);
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;
        currentHP = Mathf.Max(0, currentHP - amount);
        OnHealthChanged?.Invoke(currentHP, maxHP);

        // Spawn damage popup
        SpawnDamagePopup(amount);

        if (currentHP == 0) Die();
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