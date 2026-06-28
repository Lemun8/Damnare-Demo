using System;
using UnityEngine;

[Serializable]
public class LootEntry
{
    public ScriptableObject item; // ItemData or EquipmentData
    public int quantity = 1;
    [Range(0, 1)] public float chance = 1f; // only used if LootData.isRandomLoot == true
}
