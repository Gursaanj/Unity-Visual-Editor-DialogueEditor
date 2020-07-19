using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

public class DialogueGraphView : GraphView
{
    public readonly Vector2 DefaultNodeSize = new Vector2(150, 200);
    private const int _maxCharactersforTitle = 40;


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
        this.AddManipulator(new EdgeManipulator());


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

        //Add custom stylesheet
        node.styleSheets.Add(Resources.Load<StyleSheet>("Node"));

        //Create button to create additional Output ports
        Button AddOutputPortButton = new Button(delegate
        {
            AddOutputPort(node);
        });

        AddOutputPortButton.text = "Add output";
        node.titleButtonContainer.Add(AddOutputPortButton);

        ///Create editable Dialogue Field
        TextField dialogueField = new TextField(string.Empty);
        dialogueField.RegisterValueChangedCallback(evt =>
        {
            node.DialogueText = evt.newValue;
            node.title = evt.newValue.ToCharArray().Length > _maxCharactersforTitle ? evt.newValue.Substring(0, _maxCharactersforTitle) : evt.newValue;
        });

        dialogueField.multiline = true;
        node.mainContainer.Add(dialogueField);
        dialogueField.SetValueWithoutNotify(node.title);

        node.RefreshExpandedState();
        node.RefreshPorts();
        node.SetPosition(new Rect(Vector2.zero, DefaultNodeSize));

        return node;
    }

    /// <summary>
    /// Add Additional Output Port
    /// </summary>
    /// <param name="node"></param>
    /// <param name="overiddenPortName"></param>
    public void AddOutputPort(DialogueNode node, string overriddenPortName = "")
    {
        Port generatedPort = GeneratePort(node, Direction.Output);

        //Query based removal of OldPortNameLabels
        Label oldLabel = generatedPort.contentContainer.Q<Label>("type");
        generatedPort.contentContainer.Remove(oldLabel);

        //Gives names to the output ports
        int outputPorCount = node.outputContainer.Query("connector").ToList().Count; //way to find the number of ports
        string portName = string.IsNullOrEmpty(overriddenPortName) ?  string.Format("Choice {0}", outputPorCount) : overriddenPortName;

        //Gives the ability for users to personally rename port names on the port itself
        TextField portNameField = new TextField()
        {
            name = string.Empty,
            value = portName
        };

        portNameField.RegisterValueChangedCallback(evt => generatedPort.portName = evt.newValue);

        generatedPort.contentContainer.Add(new Label()
        {
            style = { color = Color.black, unityTextAlign = TextAnchor.MiddleLeft },
            text = " "
        });

        generatedPort.contentContainer.Add(portNameField);

        //Gives the ability for users to delete specifed ports
        Button deleteButton = new Button(delegate
        {
            RemovePort(node, generatedPort);
        })
        {
            text = "X"
        };

        deleteButton.tooltip = "Removes Port";
        generatedPort.contentContainer.Add(deleteButton);

        generatedPort.portName = portName;
        node.outputContainer.Add(generatedPort);
        node.RefreshExpandedState();
        node.RefreshPorts();
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

        //Ensure starting node is not deleteable or moveable
        node.capabilities &= ~Capabilities.Deletable;
        node.capabilities &= ~Capabilities.Movable;

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

    //Removes selected port from selected dialogue node
    private void RemovePort(DialogueNode node, Port port)
    {
        foreach (Edge edge in edges.ToList())
        {
            if (edge.output.portName == port.portName && edge.output.node == port.node)
            {
                edge.input.DisconnectAll();
                RemoveElement(edge);
                break;
            }
        }

        //Delete port whether connected or not
        node.outputContainer.Remove(port);
        node.RefreshPorts();
        node.RefreshExpandedState();
    }


}
