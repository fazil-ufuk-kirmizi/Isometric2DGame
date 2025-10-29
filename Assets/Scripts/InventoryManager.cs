using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject inventoryPanel; // Panel under Canvas
    [SerializeField] private Transform itemsParent;      // Parent with GridLayoutGroup
    [SerializeField] private GameObject itemSlotPrefab;  // Prefab containing ItemSlot
    [SerializeField, Min(1)] private int inventorySize = 20;

    [Header("Test Item Prefabs")]
    [SerializeField] private List<ItemDataSO> testItemPrefabs = new();

    private bool isInventoryOpen = false;
    private readonly List<ItemSlot> slots = new();
    private int currentTestItemIndex = 0;

    private void Start()
    {
        if (!inventoryPanel)
            Debug.LogError("Inventory Panel not assigned!");
        if (!itemsParent)
            Debug.LogError("Items Parent not assigned!");
        if (!itemSlotPrefab)
            Debug.LogError("Item Slot Prefab not assigned!");

        if (inventoryPanel) inventoryPanel.SetActive(false);
        CreateSlots();
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.iKey.wasPressedThisFrame)
            ToggleInventory();

        if (kb.tKey.wasPressedThisFrame)
            AddTestItem();
    }

    private void CreateSlots()
    {
        if (!itemsParent || !itemSlotPrefab) return;

        slots.Clear();
        for (int i = 0; i < inventorySize; i++)
        {
            var obj = Instantiate(itemSlotPrefab, itemsParent);
            var slot = obj.GetComponent<ItemSlot>();
            if (slot)
            {
                slot.ClearSlot(); // safe initialization
                slots.Add(slot);
            }
            else
            {
                Debug.LogError("ItemSlot component not on prefab!");
            }
        }
        Debug.Log($"{slots.Count} slots created");
    }

    public void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;
        if (inventoryPanel) inventoryPanel.SetActive(isInventoryOpen);
    }

    public void AddItem(Item item)
    {
        if (slots.Count == 0)
        {
            Debug.LogError("Slot list is empty!");
            return;
        }

        // 1) If stackable, first search for same name (simple stacking for example purposes)
        foreach (var s in slots)
        {
            if (s.item != null && s.item.itemName == item.itemName && s.item.icon == item.icon)
            {
                s.item.quantity += item.quantity;
                s.RefreshUI();
                Debug.Log($"{item.itemName} stacked. New quantity: {s.item.quantity}");
                return;
            }
        }

        // 2) Place in empty slot
        foreach (var s in slots)
        {
            if (s.item == null)
            {
                s.SetItem(new Item
                {
                    itemName = item.itemName,
                    icon = item.icon,
                    quantity = Mathf.Max(1, item.quantity)
                });
                Debug.Log($"{item.itemName} added!");
                return;
            }
        }

        Debug.Log("Inventory full!");
    }

    private void AddTestItem()
    {
        if (testItemPrefabs == null || testItemPrefabs.Count == 0)
        {
            Debug.LogWarning("Test Item Prefabs list is empty!");
            return;
        }

        var itemData = testItemPrefabs[currentTestItemIndex % testItemPrefabs.Count];
        currentTestItemIndex++;

        if (!itemData)
        {
            Debug.LogWarning("Selected ItemDataSO is null!");
            return;
        }

        if (!itemData.icon)
        {
            Debug.LogWarning($"No icon for {itemData.itemName}!");
            return;
        }

        var newItem = new Item
        {
            itemName = itemData.itemName,
            icon = itemData.icon,
            quantity = 1
        };

        Debug.Log($"Creating item: {newItem.itemName}, Icon: {newItem.icon.name}");
        AddItem(newItem);
    }
}