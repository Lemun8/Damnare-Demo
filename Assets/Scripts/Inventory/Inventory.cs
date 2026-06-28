using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class InventorySlot
{
    public ScriptableObject data; // ItemData or EquipmentData
    public int quantity;

    public InventorySlot(ScriptableObject data, int qty)
    {
        this.data = data;
        this.quantity = qty;
    }

    public bool IsItem => data is ItemData;
    public bool IsEquipment => data is EquipmentData;

    public ItemData AsItem() => data as ItemData;
    public EquipmentData AsEquipment() => data as EquipmentData;
}

public class Inventory : MonoBehaviour
{
    [Header("Inventory")]
    public List<InventorySlot> slots = new List<InventorySlot>();

    // Find slot index for a ScriptableObject type
    private int FindSlotIndex(ScriptableObject so)
    {
        if (so == null) return -1;
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].data == so) return i;
        }
        return -1;
    }

    // Add an item/equipment; if stackable item already present then increase qty
    public void Add(ScriptableObject so, int amount = 1)
    {
        if (so == null || amount <= 0) return;

        int idx = FindSlotIndex(so);
        if (idx >= 0)
        {
            slots[idx].quantity += amount;
        }
        else
        {
            slots.Add(new InventorySlot(so, amount));
        }
    }

    // Remove amount; returns true if removal succeeded
    public bool Remove(ScriptableObject so, int amount = 1)
    {
        if (so == null || amount <= 0) return false;

        int idx = FindSlotIndex(so);
        if (idx < 0) return false;

        InventorySlot slot = slots[idx];
        if (slot.quantity < amount) return false;

        slot.quantity -= amount;
        if (slot.quantity <= 0)
            slots.RemoveAt(idx);

        return true;
    }

    public int GetQuantity(ScriptableObject so)
    {
        int idx = FindSlotIndex(so);
        return idx >= 0 ? slots[idx].quantity : 0;
    }

    // Convenience enumerator for UI
    public List<InventorySlot> GetSlots() => slots;
}
