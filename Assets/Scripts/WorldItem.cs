using UnityEngine;

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

    private SpriteRenderer spriteRenderer;
    private Vector3 startPosition;
    private float timeOffset;

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
    }

    private void Update()
    {
        // Bob animation
        if (bobHeight > 0f)
        {
            float newY = startPosition.y + Mathf.Sin((Time.time + timeOffset) * bobSpeed) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Pickup();
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
                description = itemData.description
            };

            inventory.AddItem(newItem);
            Debug.Log($"Picked up: {newItem.itemName} x{newItem.quantity}");

            // Destroy the world item
            Destroy(gameObject);
        }
    }
}