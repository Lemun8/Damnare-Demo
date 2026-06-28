using UnityEngine;

[CreateAssetMenu(fileName = "New Dialogue", menuName = "Dialogue/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    /// <summary>Node at index 0 is always the entry point.</summary>
    public DialogueNode[] nodes;
}

[System.Serializable]
public class DialogueNode
{
    public string speakerName;
    [TextArea(2, 5)]
    public string text;
    /// <summary>Leave empty for a terminal node (closes dialogue on E).</summary>
    public DialogueChoice[] choices;
}

[System.Serializable]
public class DialogueChoice
{
    public string choiceText;
    /// <summary>Index into DialogueData.nodes. -1 ends the dialogue.</summary>
    public int nextNodeIndex = -1;
}
