using UnityEngine;

public class PlayerMind : MonoBehaviour
{
    public CharacterCombat character;

    [Header("Mind Settings")]
    public float mindDecayPerMinute = 5f;

    [Header("Runtime")]
    public float currentMind;

    float accumulator;
    bool initialized = false;

    void Awake()
    {
        if (character == null)
            character = GetComponent<CharacterCombat>();

        if (character == null)
        {
            Debug.LogError("[PlayerMind] CharacterCombat component not found!");
        }
    }

    void Start()
    {
        if (character == null) return;

        float maxMind = character.overallStats.mind;

        PlayerGameState.Load();
        
        currentMind = Mathf.Clamp(PlayerGameState.CurrentMind, 0f, maxMind);
        PlayerGameState.CurrentMind = currentMind;

        initialized = true;

        Debug.Log($"[PlayerMind] {character.characterName} initialized with {currentMind}/{maxMind} mind");
    }

    void Update()
    {
        if (!initialized || character == null || !character.isPlayerControlled) return;
        if (!IsInOverworld()) return;

        float maxMind = character.overallStats.mind;

        accumulator += Time.deltaTime;
        if (accumulator >= 1f)
        {
            float perSec = mindDecayPerMinute / 60f;
            currentMind = Mathf.Clamp(currentMind - perSec * Mathf.Floor(accumulator), 0f, maxMind);
            accumulator = 0f;
            PlayerGameState.CurrentMind = currentMind;
        }
    }

    bool IsInOverworld() => true;

    public void RestoreMind(float amt)
    {
        if (character == null || !character.isPlayerControlled) return;

        float maxMind = character.overallStats.mind;
        currentMind = Mathf.Clamp(currentMind + amt, 0f, maxMind);
        PlayerGameState.CurrentMind = currentMind;

        Debug.Log($"[PlayerMind] Restored {amt} mind. Current: {currentMind}/{maxMind}");
    }
}
