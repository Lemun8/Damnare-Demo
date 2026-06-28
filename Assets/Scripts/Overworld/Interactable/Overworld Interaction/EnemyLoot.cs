using UnityEngine;
using System.Collections.Generic;

public class EnemyLoot : MonoBehaviour
{
    [System.Serializable]
    public class Drop
    {
        public ScriptableObject item;
        public int minQty = 1;
        public int maxQty = 1;
        [Range(0, 1)] public float chance = 0.5f;
    }

    [Header("Enemy Drops")]
    public List<Drop> drops = new List<Drop>();
    public GameObject pickupPrefab; // optional: a pickup object to spawn

    private bool hasDroppedLoot = false;

    public void DropLoot()
    {
        if (hasDroppedLoot)
            return; // 🔒 Prevent multiple drops

        hasDroppedLoot = true;

        Vector3 dropPosition = transform.position + Vector3.up * 0.5f;

        foreach (var d in drops)
        {
            if (d.item == null) continue;

            if (Random.value <= d.chance)
            {
                int qty = Random.Range(d.minQty, d.maxQty + 1);

                if (pickupPrefab != null)
                {
                    var pickup = Instantiate(pickupPrefab, dropPosition, Quaternion.identity);
                    var itemPickup = pickup.GetComponent<ItemPickup>();
                    itemPickup.itemData = d.item;
                    itemPickup.quantity = qty;

                    // Spread out subsequent drops slightly
                    dropPosition += new Vector3(Random.Range(-0.3f, 0.3f), 0, 0);
                }
                else
                {
                    Debug.Log($"[Loot] Would drop {d.item.name} x{qty}");
                }
            }
        }
    }
}
