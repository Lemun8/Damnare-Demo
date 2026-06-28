using UnityEngine;

/// <summary>
/// A zone transition that only triggers once all required enemies are defeated.
/// Uses EnemyOverworldAI.uniqueID to match against WorldStateManager flags.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LockedDoor : MonoBehaviour
{
    [Header("Zone Transition")]
    public string targetScene;
    public string spawnPointName = "SceneSpawnPoint";

    [Header("Lock Conditions")]
    [Tooltip("The uniqueID value set on each required EnemyOverworldAI.")]
    public string[] requiredEnemyIDs;

    [Header("Feedback")]
    public string lockedMessage = "The door won't budge. Defeat all enemies first.";

    private bool _triggered;

    private void Start()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_triggered || !other.CompareTag("Player")) return;

        if (!AllEnemiesDefeated())
        {
            NotificationManager.Show(lockedMessage);
            return;
        }

        _triggered = true;
        other.GetComponent<PlayerController2D>()?.SetMovementEnabled(false);
        ZoneSceneLoader.Instance.StartTransition(targetScene, spawnPointName);
    }

    /// <summary>Returns true only when every required enemy has their defeat flag set.</summary>
    private bool AllEnemiesDefeated()
    {
        if (WorldStateManager.Instance == null)
        {
            Debug.LogWarning("[LockedDoor] WorldStateManager not found.");
            return false;
        }

        foreach (string id in requiredEnemyIDs)
        {
            if (!WorldStateManager.Instance.GetFlag("ENEMY_" + id))
                return false;
        }

        return true;
    }
}
