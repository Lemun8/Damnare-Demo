using UnityEngine;

public class ZoneTransitionTrigger : MonoBehaviour
{
    [Header("Scene To Load")]
    public string targetScene;

    [Header("Spawn Point in Next Scene")]
    public string spawnPointName = "SceneSpawnPoint";

    private bool triggered = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;

        if (other.CompareTag("Player"))
        {
            triggered = true;

            // Disable player movement
            other.GetComponent<PlayerController2D>().SetMovementEnabled(false);

            // Start transition
            ZoneSceneLoader.Instance.StartTransition(targetScene, spawnPointName);
        }
    }
}
