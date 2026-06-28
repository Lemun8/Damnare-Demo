using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class NPCInteractable : MonoBehaviour
{
    [Header("Dialogue")]
    public DialogueData dialogueData;

    [Header("Interaction")]
    public string interactPrompt = "Press E to talk";

    private bool _playerInRange;
    private PlayerController2D _playerController;

    // Tracks whether dialogue was active last frame to prevent same-frame re-trigger
    private bool _wasDialogueActive;

    private GUIStyle _promptStyle;
    private bool _styleInitialized;

    private void Start()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = true;
        _playerController = other.GetComponent<PlayerController2D>();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = false;
        _playerController = null;
    }

    private void Update()
    {
        bool dialogueActive = DialogueManager.Instance != null && DialogueManager.Instance.IsActive;

        // Dialogue ended this frame — skip to avoid consuming the same E keypress
        bool justEnded = _wasDialogueActive && !dialogueActive;
        _wasDialogueActive = dialogueActive;

        if (justEnded || !_playerInRange || dialogueActive) return;

        if (Input.GetKeyDown(KeyCode.E))
            TryStartDialogue();
    }

    private void TryStartDialogue()
    {
        if (dialogueData == null || DialogueManager.Instance == null) return;
        DialogueManager.Instance.StartDialogue(dialogueData, _playerController);
    }

    private void OnGUI()
    {
        if (!_playerInRange) return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsActive) return;

        InitStyle();

        const float LabelWidth = 220f;
        const float LabelHeight = 30f;
        float x = (Screen.width - LabelWidth) / 2f;
        float y = Screen.height - 200f;

        GUI.Label(new Rect(x, y, LabelWidth, LabelHeight), interactPrompt, _promptStyle);
    }

    private void InitStyle()
    {
        if (_styleInitialized) return;
        _styleInitialized = true;

        _promptStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
    }
}
