using System.Collections;
using UnityEngine;

public class OverworldSceneManager : MonoBehaviour
{
    public Transform spawnPoint;
    public CharacterPrefabLibrary prefabLibrary;

    public static GameObject PlayerInstance; // ⭐ Global reference to spawned player

    void Start()
    {
        SpawnCorrectCharacter();

        if (PlayerInstance != null)
            ApplySavedStateToPlayer(PlayerInstance);

        NotifySystems();
        StartCoroutine(HandleDefeatedEnemy()); // <-- add this
    }

    /// <summary>
    /// Finds the enemy defeated in the last battle and hides it in the overworld.
    /// Runs after one frame to ensure all enemy Start() coroutines have initialised.
    /// </summary>
    private IEnumerator HandleDefeatedEnemy()
    {
        yield return null;

        if (EnemyDataContainer.Instance == null || !EnemyDataContainer.Instance.lastEnemyDefeated)
            yield break;

        string defeatedID = EnemyDataContainer.Instance.lastEnemyID;
        if (string.IsNullOrEmpty(defeatedID))
            yield break;

        EnemyOverworldAI[] enemies = FindObjectsOfType<EnemyOverworldAI>(true);
        foreach (EnemyOverworldAI enemy in enemies)
        {
            if (enemy.uniqueID == defeatedID)
            {
                enemy.BecomeDeadAndLootable();
                enemy.DropLootOnce();
                break;
            }
        }

        EnemyDataContainer.Instance.lastEnemyDefeated = false;
    }

    void SpawnCorrectCharacter()
    {
        var selected = CharacterSelectionData.Instance.selectedClass;

        GameObject prefab = selected switch
        {
            CharacterClass.Knight  => prefabLibrary.knightOverworld,
            CharacterClass.Mage    => prefabLibrary.mageOverworld,
            CharacterClass.Archer  => prefabLibrary.archerOverworld,
            CharacterClass.Paladin => prefabLibrary.paladinOverworld,
            _ => prefabLibrary.knightOverworld,
        };

        PlayerInstance = Instantiate(prefab, spawnPoint.position, Quaternion.identity);
    }

    void ApplySavedStateToPlayer(GameObject player)
    {
        if (player == null)
        {
            Debug.LogWarning("[OverworldSceneManager] ApplySavedStateToPlayer called with null player!");
            return;
        }

        var combat = player.GetComponent<CharacterCombat>();
        var inventory = player.GetComponent<Inventory>();

        if (combat == null)
        {
            Debug.LogWarning("[OverworldSceneManager] Player prefab missing CharacterCombat component!");
        }

        if (inventory == null)
        {
            Debug.Log("[OverworldSceneManager] Player prefab missing Inventory — adding one dynamically.");
            inventory = player.AddComponent<Inventory>();
        }

        // === RESTORE COMBAT DATA ===
        if (CharacterDataContainer.Instance != null && CharacterDataContainer.Instance.playerCombatData != null && combat != null)
        {
            CharacterDataContainer.Instance.playerCombatData.ApplyTo(combat);
            Debug.Log("[OverworldSceneManager] Applied saved CharacterCombatData to player.");
        }
        else
        {
            Debug.Log("[OverworldSceneManager] No CharacterCombatData found to apply (or CharacterCombat missing).");
        }

        // === RESTORE INVENTORY ===
        if (InventoryDataContainer.Instance != null && InventoryDataContainer.Instance.savedInventorySlots != null)
        {
            // === RESTORE INVENTORY OR APPLY STARTING ITEMS ===
            if (InventoryDataContainer.Instance != null &&
                InventoryDataContainer.Instance.hasSavedInventory)
            {
                // Load saved inventory normally
                InventoryDataContainer.Instance.LoadInto(inventory);
                Debug.Log("[OverworldSceneManager] Inventory loaded from save.");
            }
            else
            {
                // No saved inventory → this is NEW GAME → APPLY PREFAB STARTING ITEMS
                Debug.Log("[OverworldSceneManager] No saved inventory — keeping prefab's starting items.");
            }
            Debug.Log("[OverworldSceneManager] Inventory loaded from InventoryDataContainer.");
        }
        else
        {
            Debug.Log("[OverworldSceneManager] No saved inventory found to load.");
        }
        
        if (combat != null)
        {
            PlayerGameState.InitializeMindForCharacter(combat);
            PlayerGameState.ApplyTo(combat);
            Debug.Log("[OverworldSceneManager] Applied PlayerGameState to player (mind/hunger).");
        }

    }

    // ⭐ Notify all systems that need player reference
    void NotifySystems()
    {
        // find every object in scene that implements the interface
        var receivers = FindObjectsOfType<MonoBehaviour>(true);

        foreach (var r in receivers)
        {
            if (r is IPlayerDependency dep)
            {
                dep.SetPlayer(PlayerInstance);
            }
        }
    }
}
