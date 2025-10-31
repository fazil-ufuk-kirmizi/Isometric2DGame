using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class WorldItem : MonoBehaviour
{
    [Header("Item Data")]
    public ItemDataSO itemData;
    public int quantity = 1;

    [Header("Visual")]
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.2f;

    [Header("Pickup")]
    [SerializeField] private bool requireKeyPress = true;  // If true, player must press E to pickup

    [Header("Tooltip")]
    [SerializeField] private GameObject tooltipPrefab;      // Assign the tooltip UI prefab
    [SerializeField] private Vector3 tooltipOffset = new Vector3(0f, 1f, 0f); // Offset above item
    [SerializeField] private string pickupText = "Press E to pick up"; // Text to display

    private SpriteRenderer spriteRenderer;
    private Vector3 startPosition;
    private float timeOffset;
    private bool playerInRange = false;
    private GameObject tooltipInstance;
    private Transform playerTransform;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        timeOffset = Random.Range(0f, 100f);
    }

    private void Start()
    {
        startPosition = transform.position;

        // Set the sprite from item data
        if (itemData && itemData.icon)
        {
            spriteRenderer.sprite = itemData.icon;
        }

        // Create tooltip if prefab is assigned
        if (tooltipPrefab && requireKeyPress)
        {
            CreateTooltip();
        }
    }

    private void Update()
    {
        // Bob animation
        if (bobHeight > 0f)
        {
            float newY = startPosition.y + Mathf.Sin((Time.time + timeOffset) * bobSpeed) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        // Update tooltip position
        if (tooltipInstance != null && tooltipInstance.activeSelf)
        {
            tooltipInstance.transform.position = transform.position + tooltipOffset;
        }

        // Check for pickup input when player is in range
        if (playerInRange && requireKeyPress)
        {
            // Using New Input System
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
            {
                Pickup();
            }
        }
    }

    private void OnDestroy()
    {
        // Clean up tooltip when item is destroyed
        if (tooltipInstance != null)
        {
            Destroy(tooltipInstance);
        }
    }

    public void Initialize(ItemDataSO data, int qty)
    {
        itemData = data;
        quantity = Mathf.Max(1, qty);

        if (spriteRenderer && itemData && itemData.icon)
        {
            spriteRenderer.sprite = itemData.icon;
        }
    }

    private void CreateTooltip()
    {
        if (!tooltipPrefab) return;

        tooltipInstance = Instantiate(tooltipPrefab);
        tooltipInstance.transform.SetParent(null); // World space tooltip
        tooltipInstance.transform.position = transform.position + tooltipOffset;

        // Set the tooltip text
        var tooltipScript = tooltipInstance.GetComponent<ItemTooltip>();
        if (tooltipScript)
        {
            tooltipScript.SetText(pickupText);
        }

        // Hide tooltip initially
        tooltipInstance.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            playerTransform = other.transform;

            // Show tooltip if using key press pickup
            if (requireKeyPress && tooltipInstance != null)
            {
                tooltipInstance.SetActive(true);
            }
            // Auto-pickup if not requiring key press
            else if (!requireKeyPress)
            {
                Pickup();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            playerTransform = null;

            // Hide tooltip
            if (tooltipInstance != null)
            {
                tooltipInstance.SetActive(false);
            }
        }
    }

    private void Pickup()
    {
        if (!itemData) return;

        // Find the inventory manager
        InventoryManager inventory = FindFirstObjectByType<InventoryManager>();
        if (inventory != null)
        {
            Item newItem = new Item
            {
                itemName = itemData.itemName,
                icon = itemData.icon,
                quantity = quantity,
                description = itemData.description,
                itemData = itemData // Keep reference to the ScriptableObject
            };

            inventory.AddItem(newItem);
            Debug.Log($"Picked up: {newItem.itemName} x{newItem.quantity}");

            // Destroy the world item
            Destroy(gameObject);
        }
    }
}