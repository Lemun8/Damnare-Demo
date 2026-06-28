using UnityEngine;
using RPG.Combat;

public class BattleSceneManager : MonoBehaviour
{
    [Header("Prefabs")]
    public CharacterPrefabLibrary prefabLibrary;
    public GameObject enemyPrefab;

    [Header("Scene References")]
    public Transform playerSpawnPoint;
    public Transform enemySpawnPoint;

    [Header("Debug Info")]
    public CharacterCombat playerCombatInstance;
    public CharacterCombat enemyCombatInstance;

    private CombatUIManager uiManager;
    private PlayerActionPlanner planner;

    void Start()
    {
        FindSceneReferences();
        SpawnCombatants();
        InitializeBattleSystems();
    }

    void FindSceneReferences()
    {
        if (playerSpawnPoint == null)
        {
            var found = GameObject.Find("PlayerSpawnPoint");
            if (found != null)
                playerSpawnPoint = found.transform;
            else
                Debug.LogWarning("[BattleSceneManager] No PlayerSpawnPoint found in scene!");
        }

        if (enemySpawnPoint == null)
        {
            var found = GameObject.Find("EnemySpawnPoint");
            if (found != null)
                enemySpawnPoint = found.transform;
            else
                Debug.LogWarning("[BattleSceneManager] No EnemySpawnPoint found in scene!");
        }

        uiManager = FindObjectOfType<CombatUIManager>();
        planner = FindObjectOfType<PlayerActionPlanner>();

        if (uiManager == null)
            Debug.LogWarning("[BattleSceneManager] CombatUIManager not found in scene!");

        if (planner == null)
            Debug.LogWarning("[BattleSceneManager] PlayerActionPlanner not found in scene!");
    }

    void SpawnCombatants()
    {
        GameObject selectedPlayerPrefab = null;

        if (prefabLibrary != null)
        {
            var selectedClass = CharacterSelectionData.Instance.selectedClass;

            selectedPlayerPrefab = selectedClass switch
            {
                CharacterClass.Knight  => prefabLibrary.knightBattle,
                CharacterClass.Mage    => prefabLibrary.mageBattle,
                CharacterClass.Archer  => prefabLibrary.archerBattle,
                CharacterClass.Paladin => prefabLibrary.paladinBattle,
                _ => prefabLibrary.knightBattle
            };
        }

        if (selectedPlayerPrefab != null && playerSpawnPoint != null)
        {
            GameObject playerObj = Instantiate(selectedPlayerPrefab, playerSpawnPoint.position, Quaternion.identity);
            playerCombatInstance = playerObj.GetComponent<CharacterCombat>();

            if (playerCombatInstance == null)
            {
                Debug.LogError("[BattleSceneManager] Player prefab is missing CharacterCombat!");
            }
            else
            {
                Debug.Log("[BattleSceneManager] Player spawned successfully.");

                // Apply saved combat stats
                if (CharacterDataContainer.Instance != null && CharacterDataContainer.Instance.playerCombatData != null)
                {
                    CharacterDataContainer.Instance.playerCombatData.ApplyTo(playerCombatInstance);
                    Debug.Log("[BattleSceneManager] Player combat data restored.");
                }

                // Restore inventory
                if (InventoryDataContainer.Instance != null)
                {
                    playerCombatInstance.LoadInventoryFromContainer();
                    Debug.Log("[BattleSceneManager] Player inventory restored.");
                }
            }
        }
        else
        {
            Debug.LogError("[BattleSceneManager] Failed to spawn player. Missing prefabLibrary or spawnPoint.");
        }

        GameObject enemyPrefabToSpawn = null;

        if (EnemyDataContainer.Instance != null)
        {
            enemyPrefabToSpawn = EnemyDataContainer.Instance.enemyBattlePrefabToSpawn;
        }

        if (enemyPrefabToSpawn == null)
        {
            Debug.LogWarning("No enemy battle prefab found! Using fallback prefab.");
            enemyPrefabToSpawn = enemyPrefab; // fallback
        }

        if (enemySpawnPoint != null)
        {
            GameObject enemyObj = Instantiate(enemyPrefabToSpawn, enemySpawnPoint.position, Quaternion.identity);
            enemyCombatInstance = enemyObj.GetComponent<CharacterCombat>();
        }
    }

    void InitializeBattleSystems()
    {
        if (playerCombatInstance == null || uiManager == null || planner == null)
        {
            Debug.LogError("[BattleSceneManager] Missing references. Cannot initialize battle systems.");
            return;
        }

        planner.Initialize(playerCombatInstance, uiManager);
        planner.BeginPlanning();

        Debug.Log("[BattleSceneManager] Battle systems initialized successfully.");
    }
}
