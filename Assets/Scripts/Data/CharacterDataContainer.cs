using UnityEngine;

public class CharacterDataContainer : MonoBehaviour
{
    public static CharacterDataContainer Instance;

    [System.NonSerialized]
    public CharacterCombatData playerCombatData; // prevent inspector serialization

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SaveCharacter(CharacterCombat character)
    {
        playerCombatData = new CharacterCombatData(character);
        
    }
}
