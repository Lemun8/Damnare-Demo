using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class CharacterSelectionUI : MonoBehaviour
{
    [Header("Class Buttons")]
    public Button knightButton;
    public Button paladinButton;
    public Button archerButton;
    public Button mageButton;

    [Header("Info UI")]
    public TextMeshProUGUI descriptionText;
    public Button confirmButton;

    private CharacterClass selectedClass;
    private bool hasSelection = false;

    void Awake()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Button bindings
        knightButton.onClick.AddListener(() => SelectClass(CharacterClass.Knight));
        paladinButton.onClick.AddListener(() => SelectClass(CharacterClass.Paladin));
        archerButton.onClick.AddListener(() => SelectClass(CharacterClass.Archer));
        mageButton.onClick.AddListener(() => SelectClass(CharacterClass.Mage));

        confirmButton.onClick.AddListener(ConfirmSelection);

        confirmButton.interactable = false;
        descriptionText.text = "Select a class to see details.";
    }

    void SelectClass(CharacterClass cls)
    {
        selectedClass = cls;
        hasSelection = true;
        confirmButton.interactable = true;

        descriptionText.text = GetDescription(cls);
    }

    void ConfirmSelection()
    {
        if (!hasSelection) return;

        Debug.Log("Selected class: " + selectedClass);

        CharacterSelectionData.Instance.selectedClass = selectedClass;

        SceneManager.LoadScene("Dungeon - Level 1");
    }

    string GetDescription(CharacterClass cls)
    {
        return cls switch
        {
            CharacterClass.Knight =>
                "Knight\n\n" +
                "A sturdy fighter who excels in melee combat.\n" +
                "Can equip all weapons.\n\n" +
                "Starts with 5 AP.\n\n" +
                "Starting equipment:\n" +
                "- Two-Handed Sword\n" +
                "- Leather Armor Set\n" +
                "- 2x Antidote\n\n" +
                "Innate skills:\n" +
                "- Intimidate", //+
                // "High Physical Attack\nNo Magic Attack\nHigh Base HP\nModerate Agility\nStarts with 4 AP.",

            CharacterClass.Paladin =>
                "Paladin\n\n" +
                "A holy warrior balanced between offense and defense.\n" +
                "Can equip all weapons.\n\n" +
                "Starts with 4 AP.\n\n" +
                "Starting equipment:\n" +
                "- One-Handed Sword\n" +
                "- Shield\n" +
                "- Plate Armor Set\n" +
                "- Elixir of Body\n\n" +
                "Innate skills:\n" +
                "- Inspire", //+
                //"Moderate Physical & Magic Attack\nHigh Defense\nLow Agility\nStarts with 3 AP.",

            CharacterClass.Archer =>
                "Archer\n\n" +
                "A ranged specialist dealing fast, precise attacks.\n" +
                "Cannot equip two-handed weapons.\n\n" +
                "Starts with 6 AP.\n\n" +
                "Starting equipment:\n" +
                "- Short Bow\n" +
                "- Dagger\n" +
                "- Fur Armor Set\n" +
                "- 3x Throwing Knives\n\n" +
                "Innate skills:\n" +
                "- Weaken", //+
                //"High Agility\nHigh Physical Attack\nLow HP\nStarts with 5 AP.",

            CharacterClass.Mage =>
                "Mage\n\n" +
                "A master of destructive and supportive magic.\n" +
                "Cannot equip two-handed weapons.\n\n" +
                "Starts with 5 AP.\n\n" +
                "Starting equipment:\n" +
                "- Magic Staff\n" +
                "- Dagger\n" +
                "- Mage Robe Set\n" +
                "- 2x Elixir of Mind\n\n" +
                "Innate skills:\n" +
                "- Heal", //+
                //"High Magic Attack\nHigh Mind\nLow Physical Stats\nStarts with 4 AP.",

            _ => "Unknown class."
        };
    }
}
