using UnityEngine;

public class CharacterSelectionData : MonoBehaviour
{
    public static CharacterSelectionData Instance;

    public CharacterClass selectedClass;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
