using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "NewLootableTile", menuName = "Loot/Lootable Tile")]
public class LootableTile : Tile
{
    public LootData lootData;
}
