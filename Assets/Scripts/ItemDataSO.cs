using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item Data")]
public class ItemDataSO : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    [TextArea]
    public string description;
}