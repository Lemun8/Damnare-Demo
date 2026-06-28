using UnityEngine;

public class BattleAudioManager : MonoBehaviour
{
    public static BattleAudioManager Instance;

    private AudioSource source;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        source = gameObject.AddComponent<AudioSource>();
        source.spatialBlend = 0f; // 2D sound
        source.playOnAwake = false;
    }

    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        source.PlayOneShot(clip, volume);
    }
}
