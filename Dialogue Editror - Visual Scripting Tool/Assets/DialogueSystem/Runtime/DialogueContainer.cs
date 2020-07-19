using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds all information in a graph pertaining to
/// all nodes and all connections
/// </summary>
[System.Serializable]
public class DialogueContainer : ScriptableObject
{
    public List<DialogueNodeData> DialogueNodeData = new List<DialogueNodeData>();
    public List<NodeLinkData> NodeLinkData = new List<NodeLinkData>();
}
