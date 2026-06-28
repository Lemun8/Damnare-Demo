using UnityEngine;
using RPG.Combat;

[System.Serializable]
public class EquipmentSlot
{
    public EquipmentType slotType;   // Armor, Weapon, Accessory
    public EquipmentData equippedItem;

    public EquipmentSlot(EquipmentType type)
    {
        slotType = type;
        equippedItem = null;
    }

    public bool IsEmpty => equippedItem == null;

    public void Equip(EquipmentData item)
    {
        equippedItem = item;
    }

    public void Unequip()
    {
        equippedItem = null;
    }
}
