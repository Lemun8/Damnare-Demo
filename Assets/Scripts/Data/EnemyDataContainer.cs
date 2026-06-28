using UnityEngine;

public class EnemyDataContainer : MonoBehaviour
{
    public static EnemyDataContainer Instance;

    public GameObject enemyBattlePrefabToSpawn;
    public string lastEnemyID;
    public bool lastEnemyDefeated;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SaveEnemy(EnemyOverworldAI enemy)
    {
        lastEnemyID = enemy.uniqueID; // was: enemy.gameObject.name
        lastEnemyDefeated = false;
        enemyBattlePrefabToSpawn = enemy.battlePrefab;
    }
}
