using UnityEngine;

public enum ItemType
{
    Material,      // Cannot be used
    Consumable,    // Can be used (healing, buffs, etc.)
    Equipment,     // Can be equipped (future implementation)
    QuestItem      // Story items
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item Data")]
public class ItemDataSO : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    [TextArea]
    public string description;

    [Header("Item Properties")]
    public ItemType itemType = ItemType.Material;

    [Header("Consumable Properties (if itemType = Consumable)")]
    public int healAmount = 0;           // Amount of HP to restore
    public bool consumeOnUse = true;     // Should the item be removed after use?
}