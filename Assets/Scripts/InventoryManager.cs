using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class InventoryManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject inventoryPanel; // Panel under Canvas
    [SerializeField] private Transform itemsParent;      // Parent with GridLayoutGroup
    [SerializeField] private GameObject itemSlotPrefab;  // Prefab containing ItemSlot
    [SerializeField, Min(1)] private int inventorySize = 20;
    [SerializeField] private Button closeButton;         // Button to close inventory

    [Header("Description Panel")]
    [SerializeField] private GameObject descriptionPanel;  // Panel to show item description
    [SerializeField] private TextMeshProUGUI itemNameText; // Text for item name
    [SerializeField] private TextMeshProUGUI descriptionText; // Text for item description

    [Header("Test Item Prefabs")]
    [SerializeField] private List<ItemDataSO> testItemPrefabs = new();

    private bool isInventoryOpen = false;
    private readonly List<ItemSlot> slots = new();
    private int currentTestItemIndex = 0;

    // Public property to check if inventory is open
    public bool IsInventoryOpen => isInventoryOpen;

    private void Start()
    {
        if (!inventoryPanel)
            Debug.LogError("Inventory Panel not assigned!");
        if (!itemsParent)
            Debug.LogError("Items Parent not assigned!");
        if (!itemSlotPrefab)
            Debug.LogError("Item Slot Prefab not assigned!");

        if (inventoryPanel) inventoryPanel.SetActive(false);

        // Hide description panel initially
        if (descriptionPanel) descriptionPanel.SetActive(false);

        // Setup close button
        if (closeButton)
        {
            closeButton.onClick.AddListener(CloseInventory);
        }
        else
        {
            Debug.LogWarning("Close Button not assigned! Assign it in the Inspector.");
        }

        CreateSlots();
    }

    private void OnDestroy()
    {
        // Clean up button listener
        if (closeButton)
        {
            closeButton.onClick.RemoveListener(CloseInventory);
        }
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.iKey.wasPressedThisFrame)
            ToggleInventory();

        if (kb.tKey.wasPressedThisFrame)
            AddTestItem();

        // ESC key also closes inventory
        if (kb.escapeKey.wasPressedThisFrame && isInventoryOpen)
            CloseInventory();
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
        if (isInventoryOpen)
        {
            CloseInventory();
        }
        else
        {
            OpenInventory();
        }
    }

    public void OpenInventory()
    {
        isInventoryOpen = true;
        if (inventoryPanel) inventoryPanel.SetActive(true);
        Debug.Log("Inventory opened");
    }

    public void CloseInventory()
    {
        isInventoryOpen = false;
        if (inventoryPanel) inventoryPanel.SetActive(false);

        // Hide description when closing inventory
        if (descriptionPanel)
        {
            descriptionPanel.SetActive(false);
        }

        Debug.Log("Inventory closed");
    }

    public void ShowItemDescription(Item item)
    {
        if (item == null || !descriptionPanel) return;

        descriptionPanel.SetActive(true);

        if (itemNameText)
        {
            itemNameText.text = item.itemName;
        }

        if (descriptionText)
        {
            descriptionText.text = string.IsNullOrEmpty(item.description)
                ? "No description available."
                : item.description;
        }

        Debug.Log($"Showing description for: {item.itemName}");
    }

    public void HideItemDescription()
    {
        if (descriptionPanel)
        {
            descriptionPanel.SetActive(false);
        }
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
                    quantity = Mathf.Max(1, item.quantity),
                    description = item.description
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
            quantity = 1,
            description = itemData.description
        };

        Debug.Log($"Creating item: {newItem.itemName}, Icon: {newItem.icon.name}");
        AddItem(newItem);
    }
}