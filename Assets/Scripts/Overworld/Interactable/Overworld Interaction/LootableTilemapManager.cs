using UnityEngine;
using UnityEngine.Tilemaps;

public class LootableTilemapManager : MonoBehaviour, IPlayerDependency
{
    public Tilemap lootTilemap;
    public Transform player;
    public float interactRange = 1.2f;

    private Inventory playerInventory;

    public void SetPlayer(GameObject playerObj)
    {
        player = playerObj.transform;
        playerInventory = playerObj.GetComponent<Inventory>();
    }

    private void Start()
    {
        RemoveAlreadyLootedTiles();
    }

    private void RemoveAlreadyLootedTiles()
    {
        if (lootTilemap == null) return;

        BoundsInt bounds = lootTilemap.cellBounds;
        foreach (var pos in bounds.allPositionsWithin)
        {
            if (!lootTilemap.HasTile(pos)) continue;
            string tileID = GetTileID(pos);
            if (WorldStateManager.Instance != null && WorldStateManager.Instance.GetFlag(tileID))
            {
                lootTilemap.SetTile(pos, null);
            }
        }
    }

    private void Update()
    {
        if (player == null) return;

        if (Input.GetKeyDown(KeyCode.Space))
            TryLoot();
    }

    private void TryLoot()
    {
        if (lootTilemap == null || player == null)
        {
            Debug.Log("[Loot] Missing tilemap or player.");
            return;
        }

        Vector3Int cellPos = lootTilemap.WorldToCell(player.position);
        TileBase tile = lootTilemap.GetTile(cellPos);

        if (tile is not LootableTile lootTile || lootTile.lootData == null)
        {
            Debug.Log("[Loot] No lootable tile found at player position.");
            return;
        }

        string tileID = GetTileID(cellPos);

        if (WorldStateManager.Instance != null && WorldStateManager.Instance.GetFlag(tileID))
        {
            Debug.Log("[Loot] Tile already looted before!");
            lootTilemap.SetTile(cellPos, null);
            return;
        }

        LootData lootData = lootTile.lootData;
        if (lootData.isRandomLoot)
        {
            GenerateRandomLoot(lootData);
        }
        else
        {
            GiveFixedLoot(lootData);
        }

        lootTilemap.SetTile(cellPos, null);

        // Persist that this tile was looted
        if (WorldStateManager.Instance != null)
            WorldStateManager.Instance.SetFlag(tileID, true);

        Debug.Log("[Loot] Tile looted and removed!");
    }

    private string GetTileID(Vector3Int pos)
    {
        // scene name + tilemap name + coordinates
        return $"{gameObject.scene.name}:{lootTilemap.name}:{pos.x}:{pos.y}:{pos.z}";
    }

    private void GiveFixedLoot(LootData lootData)
    {
        foreach (var entry in lootData.lootEntries)
        {
            if (entry.item == null || entry.quantity <= 0) continue;
            playerInventory.Add(entry.item, entry.quantity);
            NotificationManager.Show($"Got {entry.item.name} x{entry.quantity}");
        }
    }

    private void GenerateRandomLoot(LootData lootData)
    {
        if (Random.value > lootData.dropChance)
        {
            NotificationManager.Show("Nothing found this time...");
            return;
        }

        var possibleDrops = lootData.lootEntries;
        var selectedLoot = new System.Collections.Generic.List<LootEntry>();

        foreach (var entry in possibleDrops)
        {
            if (entry.item != null && Random.value <= entry.chance)
                selectedLoot.Add(entry);
        }

        int countToDrop = Mathf.Min(lootData.randomLootCount, selectedLoot.Count);

        for (int i = 0; i < countToDrop; i++)
        {
            LootEntry entry = selectedLoot[Random.Range(0, selectedLoot.Count)];
            selectedLoot.Remove(entry);
            playerInventory.Add(entry.item, entry.quantity);
            NotificationManager.Show($"Got {entry.item.name} x{entry.quantity}");
        }

        if (countToDrop == 0)
            NotificationManager.Show("You search but find nothing useful...");
    }
}
