using UnityEngine;
using UnityEngine.SceneManagement;

public class ZoneSceneLoader : MonoBehaviour
{
    public static ZoneSceneLoader Instance;

    private string nextSpawnPoint;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void StartTransition(string sceneName, string spawnPointName)
    {
        nextSpawnPoint = spawnPointName;

        // === SAVE PLAYER STATE BEFORE LEAVING THE SCENE ===
        var player = OverworldSceneManager.PlayerInstance;
        if (player != null)
        {
            var combat = player.GetComponent<CharacterCombat>();
            var inventory = player.GetComponent<Inventory>();

            if (combat != null)
                CharacterDataContainer.Instance.SaveCharacter(combat);

            if (inventory != null)
                InventoryDataContainer.Instance.SaveInventory(inventory);

            // Save hunger/mind/etc if you have a PlayerGameState system
            PlayerGameState.Save();
        }

        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private System.Collections.IEnumerator LoadSceneRoutine(string sceneName)
    {
        // fade out
        yield return ZoneFadeUI.Instance.FadeOut();

        // load scene
        yield return SceneManager.LoadSceneAsync(sceneName);

        // wait 1 frame so scene can initialize OverworldSceneManager
        yield return null;

        // move player to spawn point
        ApplySpawnPoint();

        ApplyRestoredPlayerState();

        // fade in
        yield return ZoneFadeUI.Instance.FadeIn();
    }

    void ApplyRestoredPlayerState()
    {
        GameObject player = OverworldSceneManager.PlayerInstance;
        if (player == null) return;

        var combat = player.GetComponent<CharacterCombat>();
        var inventory = player.GetComponent<Inventory>();

        if (combat != null && CharacterDataContainer.Instance.playerCombatData != null)
            CharacterDataContainer.Instance.playerCombatData.ApplyTo(combat);

        if (inventory != null)
            InventoryDataContainer.Instance.LoadInto(inventory);

        PlayerGameState.ApplyTo(combat);
    }

    void ApplySpawnPoint()
    {
        // Find the spawnpoint inside the newly loaded scene
        GameObject spawnObj = GameObject.Find(nextSpawnPoint);
        if (spawnObj == null)
        {
            Debug.LogWarning($"[ZoneLoader] SpawnPoint '{nextSpawnPoint}' not found!");
            return;
        }

        // Move the player
        var player = OverworldSceneManager.PlayerInstance;
        player.transform.position = spawnObj.transform.position;

        // Re-enable movement
        player.GetComponent<PlayerController2D>().SetMovementEnabled(true);

        // Notify systems (camera, AI, etc.)
        NotifySceneSystems(player);
    }

    void NotifySceneSystems(GameObject player)
    {
        var receivers = Object.FindObjectsOfType<MonoBehaviour>(true);
        foreach (var r in receivers)
        {
            if (r is IPlayerDependency dep)
                dep.SetPlayer(player);
        }
    }
}
