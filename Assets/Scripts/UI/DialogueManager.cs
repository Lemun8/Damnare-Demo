using UnityEngine;
using System;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    /// <summary>True while a dialogue sequence is running.</summary>
    public bool IsActive { get; private set; }

    public event Action OnDialogueEnded;

    private enum DialogueState { Talking, Choosing }

    private DialogueData _data;
    private DialogueNode _currentNode;
    private DialogueState _state;
    private int _selectedChoice;
    private PlayerController2D _player;

    private GUIStyle _boxStyle;
    private GUIStyle _nameStyle;
    private GUIStyle _textStyle;
    private GUIStyle _choiceStyle;
    private GUIStyle _choiceSelectedStyle;
    private GUIStyle _hintStyle;
    private bool _stylesInitialized;

    private const float BoxPadding = 16f;
    private const float TalkingBoxHeight = 140f;
    private const float ChoiceLineHeight = 28f;
    private const float HintHeight = 24f;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>Begins a dialogue from node 0 and locks player movement.</summary>
    public void StartDialogue(DialogueData data, PlayerController2D player)
    {
        if (IsActive || data == null || data.nodes == null || data.nodes.Length == 0) return;

        _data = data;
        _player = player;
        IsActive = true;

        _player?.SetMovementEnabled(false);
        ShowNode(0);
    }

    private void ShowNode(int index)
    {
        if (index < 0 || index >= _data.nodes.Length)
        {
            EndDialogue();
            return;
        }

        _currentNode = _data.nodes[index];
        _selectedChoice = 0;
        _state = DialogueState.Talking;
    }

    private void Update()
    {
        if (!IsActive) return;

        if (_state == DialogueState.Talking)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                bool hasChoices = _currentNode.choices != null && _currentNode.choices.Length > 0;
                if (hasChoices)
                    _state = DialogueState.Choosing;
                else
                    EndDialogue();
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
                EndDialogue();
        }
        else if (_state == DialogueState.Choosing)
        {
            int count = _currentNode.choices.Length;

            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
                _selectedChoice = (_selectedChoice - 1 + count) % count;
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
                _selectedChoice = (_selectedChoice + 1) % count;
            else if (Input.GetKeyDown(KeyCode.E))
                ConfirmChoice();
            else if (Input.GetKeyDown(KeyCode.Escape))
                EndDialogue();
        }
    }

    private void ConfirmChoice()
    {
        DialogueChoice choice = _currentNode.choices[_selectedChoice];
        if (choice.nextNodeIndex < 0)
            EndDialogue();
        else
            ShowNode(choice.nextNodeIndex);
    }

    private void EndDialogue()
    {
        IsActive = false;
        _data = null;
        _currentNode = null;

        _player?.SetMovementEnabled(true);
        _player = null;

        OnDialogueEnded?.Invoke();
    }

    private void OnGUI()
    {
        if (!IsActive || _currentNode == null) return;

        InitStyles();

        float boxWidth = Screen.width * 0.7f;
        float boxX = (Screen.width - boxWidth) / 2f;

        if (_state == DialogueState.Talking)
            DrawTalkingBox(boxX, boxWidth);
        else
            DrawChoiceBox(boxX, boxWidth);
    }

    private void DrawTalkingBox(float boxX, float boxWidth)
    {
        float boxY = Screen.height - TalkingBoxHeight - 20f;

        GUI.Box(new Rect(boxX, boxY, boxWidth, TalkingBoxHeight), GUIContent.none, _boxStyle);

        GUI.Label(
            new Rect(boxX + BoxPadding, boxY + BoxPadding, boxWidth - BoxPadding * 2, 24f),
            _currentNode.speakerName, _nameStyle);

        GUI.Label(
            new Rect(boxX + BoxPadding, boxY + BoxPadding + 28f, boxWidth - BoxPadding * 2, TalkingBoxHeight - 80f),
            _currentNode.text, _textStyle);

        bool hasChoices = _currentNode.choices != null && _currentNode.choices.Length > 0;
        string hint = hasChoices ? "Press E to respond" : "Press E to close";
        GUI.Label(
            new Rect(boxX + BoxPadding, boxY + TalkingBoxHeight - 30f, boxWidth - BoxPadding * 2, 24f),
            hint, _hintStyle);
    }

    private void DrawChoiceBox(float boxX, float boxWidth)
    {
        int count = _currentNode.choices.Length;
        float boxHeight = (count * ChoiceLineHeight) + HintHeight + BoxPadding * 2;
        float boxY = Screen.height - boxHeight - 20f;

        GUI.Box(new Rect(boxX, boxY, boxWidth, boxHeight), GUIContent.none, _boxStyle);

        for (int i = 0; i < count; i++)
        {
            bool selected = i == _selectedChoice;
            string label = (selected ? "> " : "   ") + _currentNode.choices[i].choiceText;
            GUIStyle style = selected ? _choiceSelectedStyle : _choiceStyle;

            GUI.Label(
                new Rect(boxX + BoxPadding, boxY + BoxPadding + (i * ChoiceLineHeight), boxWidth - BoxPadding * 2, ChoiceLineHeight),
                label, style);
        }

        GUI.Label(
            new Rect(boxX + BoxPadding, boxY + boxHeight - HintHeight - 4f, boxWidth - BoxPadding * 2, HintHeight),
            "W/S navigate  •  E confirm",
            _hintStyle);
    }

    private void InitStyles()
    {
        if (_stylesInitialized) return;
        _stylesInitialized = true;

        Texture2D bgTex = MakeTex(2, 2, new Color(0f, 0f, 0f, 0.85f));

        _boxStyle = new GUIStyle(GUI.skin.box) { normal = { background = bgTex } };

        _nameStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.yellow }
        };

        _textStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            wordWrap = true,
            normal = { textColor = Color.white }
        };

        _choiceStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
        };

        _choiceSelectedStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        _hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleRight,
            normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
        };
    }

    private static Texture2D MakeTex(int width, int height, Color color)
    {
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = color;

        Texture2D tex = new Texture2D(width, height);
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
}
