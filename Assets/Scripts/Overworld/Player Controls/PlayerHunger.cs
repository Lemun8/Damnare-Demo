using UnityEngine;
using RPG.Combat;

public class PlayerHunger : MonoBehaviour
{
    public CharacterCombat character;

    [Header("Hunger Settings")]
    public float maxHunger = 100f;
    public float startHunger = 100f;
    public float hungerDecayPerMinute = 5f;

    [Header("Thresholds / multipliers")]
    public float hungerThreshold = 50f;
    public float greaterHungerThreshold = 25f;
    
    [Header("Runtime (debug)")]
    public float currentHunger;
    public HungerStage hungerStage = HungerStage.Normal;

    float accumulator;

    void Awake()
    {
        if (character == null)
            character = GetComponent<CharacterCombat>();

        // LOAD persisted hunger
        PlayerGameState.Load();

        currentHunger = Mathf.Clamp(
            PlayerGameState.CurrentHunger,
            0f,
            maxHunger
        );

        hungerStage = PlayerGameState.CurrentHungerStage;
    }

    void Update()
    {
        if (character == null || !character.isPlayerControlled) return;
        if (!IsInOverworld()) return;

        accumulator += Time.deltaTime;
        if (accumulator >= 1f)
        {
            float perSec = hungerDecayPerMinute / 60f;
            currentHunger = Mathf.Clamp(currentHunger - perSec * Mathf.Floor(accumulator), 0f, maxHunger);
            accumulator = 0f;
            Evaluate();
        }
    }

    bool IsInOverworld() => true;

    public void AddHunger(float amt)
    {
        if (!character.isPlayerControlled) return;
        currentHunger = Mathf.Clamp(currentHunger + amt, 0f, maxHunger);
        Evaluate();
    }

    void Evaluate()
    {
        HungerStage newStage;

        if (currentHunger <= 0f)
        {
            newStage = HungerStage.GreaterHunger;
        }
        else if (currentHunger <= greaterHungerThreshold)
        {
            newStage = HungerStage.GreaterHunger;
        }
        else if (currentHunger <= hungerThreshold)
        {
            newStage = HungerStage.Hunger;
        }
        else
        {
            newStage = HungerStage.Normal;
        }

        // 🔔 Log ONLY if stage changed
        if (newStage != hungerStage)
        {
            hungerStage = newStage;
            PlayerGameState.CurrentHungerStage = hungerStage;

            switch (hungerStage)
            {
                case HungerStage.Normal:
                    NotificationManager.Show("🍞 You are no longer hungry");
                    break;

                case HungerStage.Hunger:
                    NotificationManager.Show("🥪 You are feeling hungry");
                    break;

                case HungerStage.GreaterHunger:
                    NotificationManager.Show("☠ You are starving");
                    break;
            }
        }

        // Clamp HP based on hunger
        if (character != null && character.isPlayerControlled)
            character.ApplyHungerHPClamp();

        // Death check AFTER logging
        if (currentHunger <= 0f)
        {
            DeathFromStarvation();
        }

        PlayerGameState.CurrentHunger = currentHunger;
        PlayerGameState.CurrentHungerStage = hungerStage;
    }

    void DeathFromStarvation()
    {
        NotificationManager.Show("[Hunger] Player starved to death!");
        // SceneManager.LoadScene("MainMenu");
    }
}

public enum HungerStage { Normal, Hunger, GreaterHunger }
