using UnityEngine;

[System.Serializable]
public class Item
{
    public string itemName;
    public Sprite icon;
    public int quantity = 1;
    public string description;

    // Reference to the original ItemDataSO for accessing properties like healAmount
    public ItemDataSO itemData;
}