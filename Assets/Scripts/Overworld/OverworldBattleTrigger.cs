using UnityEngine;
using UnityEngine.SceneManagement;

public class OverworldBattleTrigger : MonoBehaviour
{
    public CharacterCombat overworldPlayer;
    public string battleSceneName = "BattleScene";

    private EnemyOverworldAI currentEnemy; // 👈 store who triggered battle

    public void EnterBattle(EnemyOverworldAI enemy)
    {
        Debug.Log("[BattleTrigger] EnterBattle() CALLED");
        currentEnemy = enemy;

        if (overworldPlayer == null)
        {
            overworldPlayer = GameObject.FindWithTag("Player").GetComponent<CharacterCombat>();
        }

        if (overworldPlayer != null)
        {
            // ✅ Save player + enemy positions for return
            ScenePositionManager.Instance.SavePositions(overworldPlayer.transform.position, enemy);

            // ✅ Save character data
            CharacterDataContainer.Instance.SaveCharacter(overworldPlayer);
            Debug.Log("[BattleTrigger] Player data saved");
            EnemyDataContainer.Instance.SaveEnemy(enemy);

            // ✅ NEW: Save inventory
            var playerInventory = overworldPlayer.GetComponent<Inventory>();
            if (playerInventory != null)
            {
                InventoryDataContainer.Instance.SaveInventory(playerInventory);
                Debug.Log("[OverworldBattleTrigger] Inventory saved before entering battle.");
            }
            else
            {
                Debug.LogWarning("[OverworldBattleTrigger] Player has no Inventory component to save.");
            }

            SceneManager.LoadScene(battleSceneName);
        }
        else
        {
            Debug.LogError("[OverworldBattleTrigger] No player found to enter battle!");
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            EnterBattle(null);
        }
    }
}
