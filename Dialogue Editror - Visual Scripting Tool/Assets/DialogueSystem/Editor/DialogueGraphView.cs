using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

public class DialogueGraphView : GraphView
{
    private readonly Vector2 _defaultNodeSize = new Vector2(150, 200);


    /// <summary>
    /// Manipulators are the base class for interactions with Visual Elements (in this case, a graphview and Nodes)
    /// They can handle intercation based callbacks (built in ones like On Drag/Drop, MouseClicks and so on)
    /// </summary>

    public DialogueGraphView()
    {
        //Allow for Zooming capabilites
        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());


        //Add GraphBackground
        styleSheets.Add(Resources.Load<StyleSheet>("DialogueGraph"));
        GridBackground grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();


        //Add Starting Node
        AddElement(GenerateEntryPointNode());
    }

    public void CreateNode(string nodeName)
    {
        AddElement(CreateDialogueNode(nodeName));
    }

    /// <summary>
    /// Decides which ports can connect with eachother based on the accepting NodeAdapter
    /// </summary>
    /// <param name="startPort"></param>
    /// <param name="nodeAdapter"></param>
    /// <returns></returns>
    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        List<Port> compatiblePorts = new List<Port>();
        ports.ForEach(port =>
        {
            if (startPort != port && startPort.node != port.node)
            {
                compatiblePorts.Add(port);
            }
        });

        return compatiblePorts;
    }

    public DialogueNode CreateDialogueNode(string nodeName)
    {
        DialogueNode node = new DialogueNode
        {
            title = nodeName,
            GUID = Guid.NewGuid().ToString(),
            DialogueText = nodeName
        };

        Port inputPort = GeneratePort(node, Direction.Input, Port.Capacity.Multi);
        inputPort.portName = "Input";
        node.inputContainer.Add(inputPort);

        //Create button to create additional Output ports
        Button AddOutputPortButton = new Button(delegate
        {
            AddOutputPort(node);
        });

        AddOutputPortButton.text = "Add output";
        node.titleButtonContainer.Add(AddOutputPortButton);


        node.RefreshExpandedState();
        node.RefreshPorts();
        node.SetPosition(new Rect(Vector2.zero, _defaultNodeSize));

        return node;
    }

    private Node GenerateEntryPointNode()
    {
        DialogueNode node = new DialogueNode
        {
            title = "Start",
            GUID = Guid.NewGuid().ToString(),
            DialogueText = string.Empty,
            EntryPoint = true
        };

        Port startingPort = GeneratePort(node, Direction.Output);
        startingPort.portName = "Begin"; //Name used for when saving/loading additional graphs
        node.outputContainer.Add(startingPort);

        node.RefreshExpandedState();
        node.RefreshPorts();

        node.SetPosition(new Rect(100, 200, 100, 150));

        return node;
    }

    private Port GeneratePort(DialogueNode node, Direction portDirection, Port.Capacity capacity = Port.Capacity.Single)
    {
        // Type is needed for when Ports transmit data with other ports, but primarily, the type can be abitrary
        return node.InstantiatePort(Orientation.Horizontal, portDirection, capacity, typeof(float));
    }

    private void AddOutputPort(DialogueNode node)
    {
        Port GeneratedPort = GeneratePort(node, Direction.Output);

        //Gives names to the output ports
        int outputPorCount = node.outputContainer.Query("connector").ToList().Count; //way to find the number of ports
        GeneratedPort.portName = string.Format("Choice {0}", outputPorCount);

        node.outputContainer.Add(GeneratedPort);
        node.RefreshExpandedState();
        node.RefreshPorts();
    }

}
