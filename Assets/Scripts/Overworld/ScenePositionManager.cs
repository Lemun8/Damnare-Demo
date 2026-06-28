using UnityEngine;

public class ScenePositionManager : MonoBehaviour
{
    public static ScenePositionManager Instance;

    [Header("Saved Positions")]
    public Vector3 playerPosition;
    public Vector3 enemyPosition;
    public string enemyID;
    public bool hasSavedPositions = false;

    public string overworldSceneName;

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

    public void SavePositions(Vector3 playerPos, EnemyOverworldAI enemy)
    {
        playerPosition = playerPos;
        enemyPosition = enemy.transform.position;
        enemyID = enemy.gameObject.name;

        // NEW: Save the current overworld scene
        overworldSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        hasSavedPositions = true;
    }
}
