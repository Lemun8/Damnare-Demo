using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ItemPickup : MonoBehaviour
{
    public ScriptableObject itemData;
    public int quantity = 1;

    private bool playerInRange = false;
    private Inventory playerInventory;

    void Start()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            playerInventory = other.GetComponent<Inventory>();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            playerInventory = null;
        }
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.Space))
        {
            Pickup();
        }
    }

    private void Pickup()
    {
        if (playerInventory == null || itemData == null) return;

        playerInventory.Add(itemData, quantity);
        NotificationManager.Show($"Got {itemData.name} x{quantity}");
        Destroy(gameObject);
    }
}
