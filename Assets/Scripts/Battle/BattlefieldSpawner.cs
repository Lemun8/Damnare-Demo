using UnityEngine;

/// <summary>
/// Placed in BattleScene. Randomly picks one battlefield environment prefab on load.
/// </summary>
public class BattlefieldSpawner : MonoBehaviour
{
    [Header("Battlefield Environment Prefabs")]
    public GameObject[] battlefieldPrefabs;

    [Tooltip("Optional parent transform for the spawned environment. Spawns at world origin if left empty.")]
    public Transform spawnParent;

    private void Start()
    {
        SpawnRandomBattlefield();
    }

    private void SpawnRandomBattlefield()
    {
        if (battlefieldPrefabs == null || battlefieldPrefabs.Length == 0)
        {
            Debug.LogWarning("[BattlefieldSpawner] No battlefield prefabs assigned.");
            return;
        }

        int index = Random.Range(0, battlefieldPrefabs.Length);
        GameObject selected = battlefieldPrefabs[index];

        if (selected == null)
        {
            Debug.LogWarning($"[BattlefieldSpawner] Prefab at index {index} is null.");
            return;
        }

        Vector3 pos = spawnParent != null ? spawnParent.position : Vector3.zero;
        Quaternion rot = spawnParent != null ? spawnParent.rotation : Quaternion.identity;

        Instantiate(selected, pos, rot);
        Debug.Log($"[BattlefieldSpawner] Spawned battlefield: {selected.name}");
    }
}
