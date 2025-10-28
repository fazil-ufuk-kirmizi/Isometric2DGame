using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHP = 100;
    [SerializeField] private int currentHP;

    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;

    private void Awake()
    {
        currentHP = maxHP;
    }

    public void TakeDamage(int amount)
    {
        currentHP = Mathf.Max(0, currentHP - amount);
        if (currentHP == 0)
        {
            Debug.Log("Player died.");
        }
    }
}
