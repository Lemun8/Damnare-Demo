using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewLootData", menuName = "Loot/Loot Data")]
public class LootData : ScriptableObject
{
    [Header("Loot Behavior")]
    public bool isRandomLoot = false;
    [Tooltip("How many random items should be chosen if this is random loot.")]
    public int randomLootCount = 1;

    [Header("Loot Pool (Fixed or Random)")]
    public List<LootEntry> lootEntries = new List<LootEntry>();

    [Header("Random Drop Chances (for random loot only)")]
    [Range(0, 1)] public float dropChance = 1f; // chance that any loot is given at all
}
