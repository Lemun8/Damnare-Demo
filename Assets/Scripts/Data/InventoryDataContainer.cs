using System.Collections.Generic;
using UnityEngine;

public class InventoryDataContainer : MonoBehaviour
{
    public static InventoryDataContainer Instance;
    public List<InventorySlot> savedInventorySlots = new List<InventorySlot>();

    public bool hasSavedInventory => savedInventorySlots.Count > 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SaveInventory(Inventory sourceInventory)
    {
        savedInventorySlots.Clear();
        foreach (var slot in sourceInventory.slots)
        {
            savedInventorySlots.Add(new InventorySlot(slot.data, slot.quantity));
        }
    }

    public void LoadInto(Inventory targetInventory)
    {
        targetInventory.slots.Clear();
        foreach (var slot in savedInventorySlots)
        {
            targetInventory.slots.Add(new InventorySlot(slot.data, slot.quantity));
        }
    }

    public void Clear()
    {
        savedInventorySlots.Clear();
    }
}
