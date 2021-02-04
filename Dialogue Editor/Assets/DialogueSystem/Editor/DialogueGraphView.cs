using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

public class DialogueGraphView : GraphView
{
    public readonly Vector2 DefaultNodeSize = new Vector2(150, 200);
    private const int MaxCharactersforTitle = 40;
    
    public List<ExposedProperty> ExposedProperties = new List<ExposedProperty>();
    public Blackboard Blackboard;
    private NodeSearchWindow _searchWindow;

    /// <summary>
    /// Manipulators are the base class for interactions with Visual Elements (in this case, a graphview and Nodes)
    /// They can handle intercation based callbacks (built in ones like On Drag/Drop, MouseClicks and so on)
    /// </summary>

    public DialogueGraphView(EditorWindow window)
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
        //Add Search Window
        AddSearchWindow(window);
    }
    
    public void CreateNode(string nodeName, Vector2 position)
    {
        AddElement(CreateDialogueNode(nodeName, position));
    }
    
    private void AddSearchWindow(EditorWindow window)
    {
        _searchWindow = ScriptableObject.CreateInstance<NodeSearchWindow>();
        _searchWindow.Init(window,this);
        nodeCreationRequest = delegate(NodeCreationContext context)
        {
            SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), _searchWindow);
        };
            
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

    public DialogueNode CreateDialogueNode(string nodeName, Vector2 position)
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
            node.title = evt.newValue.ToCharArray().Length > MaxCharactersforTitle ? evt.newValue.Substring(0, MaxCharactersforTitle) : evt.newValue;
        });

        dialogueField.multiline = true;
        node.mainContainer.Add(dialogueField);
        dialogueField.SetValueWithoutNotify(node.title);

        node.RefreshExpandedState();
        node.RefreshPorts();
        node.SetPosition(new Rect(position, DefaultNodeSize));

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

    public void ClearBlackBoard()
    {
        ExposedProperties.Clear();
        Blackboard.Clear();
    }

    public void AddPropertyToBlackBoard(ExposedProperty exposedProperty)
    {
        int duplicateNameIndex = 1;
        string localPropertyName = exposedProperty.PropertyName;
        string localPropertyValue = exposedProperty.PropertyValue;

        while (ExposedProperties.Any(x => x.PropertyName == localPropertyName))
        {
            localPropertyName = $"{localPropertyName}{duplicateNameIndex}"; // Name || Name1 || Name12
            duplicateNameIndex++;
        }
        
        ExposedProperty property = new ExposedProperty();
        property.PropertyName = localPropertyName;
        property.PropertyValue = localPropertyValue;
        ExposedProperties.Add(property);
        
        VisualElement container = new VisualElement();
        BlackboardField field = new BlackboardField
        {
            text = property.PropertyName,
            typeText = "string",
        };
        
        container.Add(field);

        TextField propertyValueTextField = new TextField("Value : ")
        {
            value = localPropertyValue,
            style = {width = new Length(80, LengthUnit.Percent)}
        };
        
        
        propertyValueTextField.RegisterValueChangedCallback(delegate(ChangeEvent<string> evt)
        {
            int changingPropertyIndex = ExposedProperties.FindIndex(x => x.PropertyName == property.PropertyName);
            ExposedProperties[changingPropertyIndex].PropertyValue = evt.newValue;
        });

        BlackboardRow blackboardRow = new BlackboardRow(field, propertyValueTextField);
        container.Add(blackboardRow);


        if (Blackboard != null)
        {
            Blackboard.Add(container);
        }
    }
}
