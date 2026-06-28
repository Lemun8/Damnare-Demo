using UnityEngine;

public static class PlayerGameState
{
    public static HungerStage CurrentHungerStage = HungerStage.Normal;
    public static float CurrentHunger = 100f;
    public static float CurrentMind = 100f;

    public static void Save()
    {
        PlayerPrefs.SetFloat("playerMind", CurrentMind);
        PlayerPrefs.SetFloat("playerHungerValue", CurrentHunger);
        PlayerPrefs.SetInt("playerHungerStage", (int)CurrentHungerStage);
        PlayerPrefs.Save();
    }

    public static void Load()
    {
        if (PlayerPrefs.HasKey("playerMind"))
            CurrentMind = PlayerPrefs.GetFloat("playerMind");

        if (PlayerPrefs.HasKey("playerHungerValue"))
            CurrentHunger = PlayerPrefs.GetFloat("playerHungerValue");

        if (PlayerPrefs.HasKey("playerHungerStage"))
            CurrentHungerStage = (HungerStage)PlayerPrefs.GetInt("playerHungerStage");
    }

    public static void InitializeMindForCharacter(CharacterCombat character)
    {
        if (character == null) return;
        
        float maxMind = character.overallStats.mind;
        
        if (!PlayerPrefs.HasKey("playerMind"))
        {
            CurrentMind = maxMind;
            Debug.Log($"[PlayerGameState] Initialized mind to {maxMind} for new game");
        }
        else
        {
            CurrentMind = Mathf.Clamp(CurrentMind, 0f, maxMind);
            Debug.Log($"[PlayerGameState] Loaded mind {CurrentMind}/{maxMind}");
        }
    }

    public static void ApplyTo(CharacterCombat combat)
    {
        if (combat == null) return;

        Load();

        combat.currentMind = Mathf.Clamp((int)CurrentMind, 0, combat.maxMind);

        if (combat.isPlayerControlled)
            combat.ApplyHungerHPClamp();
    }

    public static void ResetToDefaults()
    {
        CurrentHungerStage = HungerStage.Normal;
        CurrentHunger = 100f;
        CurrentMind = 100f;
        
        PlayerPrefs.DeleteKey("playerMind");
        PlayerPrefs.DeleteKey("playerHungerValue");
        PlayerPrefs.DeleteKey("playerHungerStage");
        PlayerPrefs.Save();
        
        Debug.Log("[PlayerGameState] Reset to defaults");
    }
}
