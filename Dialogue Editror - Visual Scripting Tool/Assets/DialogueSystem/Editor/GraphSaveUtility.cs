using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

/// <summary>
/// Allows the save of Graphview based System
/// </summary>
public class GraphSaveUtility
{
    private DialogueContainer _containerCache;
    private DialogueGraphView _targetGraphView;
    private List<Edge> _edges => _targetGraphView.edges.ToList();
    private List<DialogueNode> _nodes => _targetGraphView.nodes.ToList().Cast<DialogueNode>().ToList();

    private const string _graphViewFolderPath = "Assets/Resources";

    public static GraphSaveUtility GetInstance(DialogueGraphView targetGraphView)
    {
        return new GraphSaveUtility
        {
            _targetGraphView = targetGraphView,
        };
    }

    /// <summary>
    /// Save the Graph
    /// </summary>
    /// <param name="fileName"></param>
    public void SaveGraph(string fileName)
    {
        if (_edges == null || _edges.Count == 0 || _nodes == null || _nodes.Count == 0)
        {
            return;
        }

        DialogueContainer container = ScriptableObject.CreateInstance<DialogueContainer>();

        /// Save alll Connections
        foreach (Edge edge in _edges)
        {
            if (edge != null && edge.input.node != null)
            {
                DialogueNode outputNode = edge.output.node as DialogueNode;
                DialogueNode inputNode = edge.input.node as DialogueNode;

                container.NodeLinkData.Add(new NodeLinkData
                {
                    BaseNodeGuid = outputNode.GUID,
                    PortName = edge.output.portName,
                    TargetNodeGuid = inputNode.GUID
                });
            }
        }

        /// Save all Nodes
        foreach (DialogueNode node in _nodes)
        {
            if (!node.EntryPoint)
            {
                container.DialogueNodeData.Add(new DialogueNodeData
                {
                    Guid = node.GUID,
                    DialogueText = node.DialogueText,
                    Position = node.GetPosition().position
                });
            }
        }

        // If folder does not exist, then create it
        if (!AssetDatabase.IsValidFolder(_graphViewFolderPath))
        {
            int lastFolderIndex = _graphViewFolderPath.LastIndexOf('/');
            if (lastFolderIndex != -1)
            {
                AssetDatabase.CreateFolder(_graphViewFolderPath.Substring(0, lastFolderIndex), _graphViewFolderPath.Substring(lastFolderIndex + 1));
            }
            else
            {
                Debug.LogError("Unable to locate/create appropriate folder for saving");
                return;
            }
        }

        AssetDatabase.CreateAsset(container, string.Format("{0}/{1}.asset", _graphViewFolderPath, fileName));
    }

    /// <summary>
    /// Loads the Graph
    /// </summary>
    /// <param name="fileName"></param>
    public void LoadGraph(string fileName)
    {
        _containerCache = Resources.Load<DialogueContainer>(fileName);

        if (_containerCache == null)
        {
            EditorUtility.DisplayDialog("File cannot be loaded", "File does not seem to exist in the appropriate folder!", "Dammit");
            return;
        }

        // Steps to Load Graph
        ClearGraph();
        GenerateNodes();
        ConnectNodes();
    }

    /// <summary>
    /// Clears the current Graph state
    /// </summary>
    private void ClearGraph()
    {
        if (_nodes == null)
        {
            return;
        }

        //Set  entry points guid back from the saved graph. Discars existing Guid
        _nodes.Find(dialogueNode => dialogueNode.EntryPoint).GUID = _containerCache.NodeLinkData[0].BaseNodeGuid;

        foreach (DialogueNode node in _nodes)
        {
            if (node.EntryPoint)
            {
                continue;
            }

            //Remove all edges from the graph
            foreach (Edge edge in _edges)
            {
                if (edge.input.node == node)
                {
                    _targetGraphView.RemoveElement(edge);
                }
            }

            //Then remove the node itself
            _targetGraphView.RemoveElement(node);
        }
    }

    /// <summary>
    /// Generates all nodes based on loaded Graph
    /// </summary>
    private void GenerateNodes()
    {
        if (_containerCache.DialogueNodeData == null)
        {
            Debug.LogError("No Node Data to load");
            return;
        }

        //Create each saved DialogueNode
        foreach (DialogueNodeData nodeDataElement in _containerCache.DialogueNodeData)
        {
            DialogueNode tempNode = _targetGraphView.CreateDialogueNode(nodeDataElement.DialogueText);
            tempNode.GUID = nodeDataElement.Guid;
            _targetGraphView.AddElement(tempNode);

            //Create all the necessary output ports foreach DialogueNode
            foreach (NodeLinkData linkDataElement in _containerCache.NodeLinkData)
            {
                if (linkDataElement.BaseNodeGuid == nodeDataElement.Guid)
                {
                    _targetGraphView.AddOutputPort(tempNode, linkDataElement.PortName);
                }
            }
        }
    }

    /// <summary>
    /// Connects all nodes based on loaded Graph
    /// </summary>
    private void ConnectNodes()
    {
        for (var i = 0; i < _nodes.Count; i++)
        {
            var k = i; //Prevent access to modified closure
            var connections = _containerCache.NodeLinkData.Where(x => x.BaseNodeGuid == _nodes[k].GUID).ToList();
            for (var j = 0; j < connections.Count(); j++)
            {
                var targetNodeGUID = connections[j].TargetNodeGuid;
                var targetNode = _nodes.First(x => x.GUID == targetNodeGUID);
                LinkNodes(_nodes[i].outputContainer[j].Q<Port>(), (Port)targetNode.inputContainer[0]);

                targetNode.SetPosition(new Rect(
                    _containerCache.DialogueNodeData.First(x => x.Guid == targetNodeGUID).Position,
                    _targetGraphView.DefaultNodeSize));
            }
        }

    }

    private void LinkNodes(Port outputPort, Port targetInputPort)
    {
        Edge tempEdge = new Edge
        {
            output = outputPort,
            input = targetInputPort
        };

        tempEdge?.input.Connect(tempEdge);
        tempEdge?.output.Connect(tempEdge);
        _targetGraphView.AddElement(tempEdge);
    } 
}
