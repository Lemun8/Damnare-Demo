using UnityEngine;

public class OverworldSceneRestorer : MonoBehaviour
{
    void Start()
    {
        // Safety check
        if (!ScenePositionManager.Instance || !ScenePositionManager.Instance.hasSavedPositions)
            return;

        var posManager = ScenePositionManager.Instance;
        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            player.transform.position = posManager.playerPosition;
        }

        // Restore enemy position if still exists
        if (!string.IsNullOrEmpty(posManager.enemyID))
        {
            var enemyObj = GameObject.Find(posManager.enemyID);
            if (enemyObj != null)
            {
                enemyObj.transform.position = posManager.enemyPosition;

                if (EnemyDataContainer.Instance.lastEnemyDefeated)
                {
                    enemyObj.GetComponent<EnemyOverworldAI>().BecomeDeadAndLootable();
                }
                else
                {
                    // 🧊 Freeze enemy for a short duration after returning from battle
                    var enemyAI = enemyObj.GetComponent<EnemyOverworldAI>();
                    if (enemyAI != null)
                        enemyAI.FreezeTemporarily(10f); // ⏱ adjust duration as needed
                }
            }
        }

        // Clear after restoring
        posManager.hasSavedPositions = false;
    }
}
