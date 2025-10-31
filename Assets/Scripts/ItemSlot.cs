using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(LayoutElement))]
[RequireComponent(typeof(Image))]
public class ItemSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
{
    [Header("UI Refs")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private string iconChildName = "Icon";

    [HideInInspector] public Item item;
    [HideInInspector] public Transform originalParent;

    private Canvas rootCanvas;
    private CanvasGroup canvasGroup;
    private LayoutElement layoutElement;
    private RectTransform rt;
    private Image slotBackground;
    private CanvasRenderer iconRenderer;

    // Drag visual
    private GameObject dragVisual;
    private Image dragIcon;
    private RectTransform dragRT;

    private bool isDragging = false;


    private void Start()
    {
        RefreshUI();
    }

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        layoutElement = GetComponent<LayoutElement>();

        slotBackground = GetComponent<Image>();
        if (slotBackground != null)
        {
            slotBackground.raycastTarget = true;
        }
        else
        {
            Debug.LogError($"{name}: ItemSlot has no Image component! Drag won't work.");
        }

        rootCanvas = GetComponentInParent<Canvas>();
        if (!rootCanvas)
        {
            Debug.LogError($"{name}: Canvas not found. Slot must be under a Canvas.");
        }

        // Automatic icon finding
        if (!itemIcon)
        {
            var t = transform.Find(iconChildName);
            if (t) itemIcon = t.GetComponent<Image>();

            if (!itemIcon)
            {
                var imgs = GetComponentsInChildren<Image>(true);
                foreach (var img in imgs)
                {
                    if (img.gameObject != this.gameObject)
                    {
                        itemIcon = img;
                        break;
                    }
                }
            }
        }

        if (!itemIcon)
        {
            Debug.LogError($"{name}: itemIcon not found/assigned!");
        }
        else
        {
            iconRenderer = itemIcon.GetComponent<CanvasRenderer>();
            itemIcon.gameObject.SetActive(true);
            itemIcon.enabled = true;

            if (iconRenderer != null)
            {
                iconRenderer.SetAlpha(1f);
            }

            itemIcon.color = new Color(1f, 1f, 1f, 1f);
            itemIcon.raycastTarget = false;
        }

        if (!quantityText)
        {
            quantityText = GetComponentInChildren<TextMeshProUGUI>();
        }

        if (item == null && quantityText)
        {
            quantityText.text = "";
        }
    }

    public void SetItem(Item newItem)
    {
        item = newItem;
        ForceEnableIcon();
        RefreshUI();
    }

    private void ForceEnableIcon()
    {
        if (!itemIcon) return;

        if (!itemIcon.gameObject.activeSelf)
            itemIcon.gameObject.SetActive(true);

        if (!itemIcon.enabled)
            itemIcon.enabled = true;

        if (iconRenderer != null)
        {
            iconRenderer.SetAlpha(1f);
        }
        else
        {
            iconRenderer = itemIcon.GetComponent<CanvasRenderer>();
            if (iconRenderer != null)
                iconRenderer.SetAlpha(1f);
        }

        var col = itemIcon.color;
        col.a = 1f;
        itemIcon.color = col;
    }

    public void RefreshUI()
    {
        if (!itemIcon)
        {
            Debug.LogError($"[{name}] itemIcon reference missing!");
            return;
        }

        if (item == null)
        {
            itemIcon.sprite = null;
            itemIcon.color = new Color(1f, 1f, 1f, 0f);

            if (iconRenderer != null)
            {
                iconRenderer.SetAlpha(0f);
            }

            if (quantityText) quantityText.text = "";
            return;
        }

        ForceEnableIcon();

        itemIcon.color = Color.white;
        itemIcon.preserveAspect = true;

        if (item.icon)
        {
            itemIcon.sprite = item.icon;

            if (itemIcon.rectTransform.sizeDelta == Vector2.zero)
            {
                itemIcon.SetNativeSize();
            }

            if (iconRenderer != null)
            {
                iconRenderer.SetAlpha(1f);
            }
        }
        else
        {
            itemIcon.sprite = null;
            itemIcon.color = new Color(1f, 0.3f, 0.3f, 1f);
            Debug.LogWarning($"[ItemSlot:{name}] Icon not assigned for '{item.itemName}'!");
        }

        if (quantityText)
        {
            quantityText.text = (item.quantity > 1) ? item.quantity.ToString() : "";
        }
    }

    public void ClearSlot()
    {
        item = null;

        if (itemIcon)
        {
            itemIcon.sprite = null;
            itemIcon.color = new Color(1f, 1f, 1f, 0f);

            if (iconRenderer != null)
            {
                iconRenderer.SetAlpha(0f);
            }
        }

        if (quantityText) quantityText.text = "";
    }

    // --- CLICK DETECTION ---
    public void OnPointerClick(PointerEventData eventData)
    {
        if (item == null || isDragging) return;

        var inventoryManager = FindFirstObjectByType<InventoryManager>();
        if (inventoryManager == null) return;

        // Left click: Show description
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            inventoryManager.ShowItemDescription(item);
        }
        // Right click: Use item
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            inventoryManager.UseItem(this);
        }
    }

    // --- DRAG & DROP ---
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (item == null)
        {
            Debug.LogWarning($"[{name}] Drag could not be started: Item null");
            isDragging = false;
            return;
        }

        if (rootCanvas == null)
        {
            Debug.LogError($"[{name}] Drag could not be started: Canvas null");
            isDragging = false;
            return;
        }

        isDragging = true;

        Debug.Log($"[{name}] OnBeginDrag - Item: {item.itemName} (Q:{item.quantity})");

        // Create drag visual (only the icon)
        CreateDragVisual();

        // Make original icon semi-transparent to show it's being dragged
        if (itemIcon)
        {
            var col = itemIcon.color;
            col.a = 0.3f;
            itemIcon.color = col;
        }

        // Slot stays in place, only visual follows cursor
        canvasGroup.blocksRaycasts = false;
    }

    private void CreateDragVisual()
    {
        if (!itemIcon || item == null) return;

        // Create a temporary GameObject for the drag visual
        dragVisual = new GameObject("DragIcon");
        dragVisual.transform.SetParent(rootCanvas.transform, false);
        dragVisual.transform.SetAsLastSibling();

        dragRT = dragVisual.AddComponent<RectTransform>();
        dragIcon = dragVisual.AddComponent<Image>();

        // Copy icon properties
        dragIcon.sprite = itemIcon.sprite;
        dragIcon.raycastTarget = false;
        dragIcon.preserveAspect = true;

        // Set size to match original icon
        dragRT.sizeDelta = itemIcon.rectTransform.sizeDelta;

        // Semi-transparent
        var col = dragIcon.color;
        col.a = 0.6f;
        dragIcon.color = col;

        // Add CanvasGroup for better control
        var cg = dragVisual.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || item == null || rootCanvas == null || !dragRT) return;

        // Move only the drag visual
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)rootCanvas.transform,
            eventData.position,
            rootCanvas.worldCamera,
            out Vector2 localPoint))
        {
            dragRT.anchoredPosition = localPoint;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        Debug.Log($"[{name}] OnEndDrag - Item: {(item != null ? item.itemName : "NULL")}");

        // Destroy drag visual
        if (dragVisual != null)
        {
            Destroy(dragVisual);
            dragVisual = null;
            dragRT = null;
            dragIcon = null;
        }

        // Restore original icon - only if item still exists
        if (itemIcon && item != null)
        {
            var col = itemIcon.color;
            col.a = 1f;
            itemIcon.color = col;

            if (iconRenderer != null)
            {
                iconRenderer.SetAlpha(1f);
            }
        }

        canvasGroup.blocksRaycasts = true;
        isDragging = false;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
        {
            Debug.LogWarning($"[{name}] OnDrop: pointerDrag null");
            return;
        }

        var draggedSlot = eventData.pointerDrag.GetComponent<ItemSlot>();

        if (draggedSlot == null)
        {
            Debug.LogWarning($"[{name}] OnDrop: ItemSlot component not found");
            return;
        }

        if (draggedSlot == this)
        {
            Debug.Log($"[{name}] OnDrop: Dropped on the same slot");
            return;
        }

        // SAVE REFERENCES (important!)
        Item thisItem = this.item;
        Item draggedItem = draggedSlot.item;

        Debug.Log($"[{name}] OnDrop - This slot: {(thisItem != null ? thisItem.itemName : "empty")}, " +
                  $"Dragged: {(draggedItem != null ? draggedItem.itemName : "empty")}");

        // If both slots are empty, do nothing
        if (thisItem == null && draggedItem == null)
        {
            Debug.Log($"[{name}] Both slots are empty");
            return;
        }

        // If same item type (using ItemDataSO reference), stack them
        if (thisItem != null && draggedItem != null &&
            thisItem.itemData != null && draggedItem.itemData != null &&
            thisItem.itemData == draggedItem.itemData)
        {
            Debug.Log($"[{name}] Item stacking: {thisItem.itemName} ({thisItem.quantity} + {draggedItem.quantity})");

            thisItem.quantity += draggedItem.quantity;
            draggedSlot.ClearSlot(); // Clear the dragged slot
            this.RefreshUI(); // Update this slot
            return;
        }

        // SWAP OPERATION - Correct order
        Debug.Log($"[{name}] Item swap in progress");

        // First clear the dragged slot
        draggedSlot.item = null;
        draggedSlot.RefreshUI();

        // Then clear this slot
        this.item = null;
        this.RefreshUI();

        // Now assign new values
        if (draggedItem != null)
        {
            this.SetItem(draggedItem);
        }

        if (thisItem != null)
        {
            draggedSlot.SetItem(thisItem);
        }

        Debug.Log($"[{name}] Swap completed - This slot: {(this.item != null ? this.item.itemName : "empty")}, " +
                  $"Dragged slot: {(draggedSlot.item != null ? draggedSlot.item.itemName : "empty")}");
    }

    // --- DEBUG ---
    [ContextMenu("TEST: Force Show Icon")]
    private void TestForceShow()
    {
        if (!itemIcon)
        {
            Debug.LogError($"[{name}] itemIcon NULL!");
            return;
        }

        ForceEnableIcon();

        var cr = itemIcon.GetComponent<CanvasRenderer>();
        Debug.Log($"[{name}] " +
                  $"GameObject.active={itemIcon.gameObject.activeSelf}, " +
                  $"Image.enabled={itemIcon.enabled}, " +
                  $"Color={itemIcon.color}, " +
                  $"Sprite={(itemIcon.sprite ? itemIcon.sprite.name : "NULL")}, " +
                  $"CanvasRenderer={(cr != null ? $"Alpha={cr.GetAlpha()}" : "NULL")}");
    }

    [ContextMenu("TEST: Show Current Item")]
    private void TestShowCurrentItem()
    {
        if (item != null)
        {
            Debug.Log($"[{name}] Item: {item.itemName}, Quantity: {item.quantity}, Icon: {(item.icon ? item.icon.name : "NULL")}");
        }
        else
        {
            Debug.Log($"[{name}] Item: NULL");
        }
    }
}