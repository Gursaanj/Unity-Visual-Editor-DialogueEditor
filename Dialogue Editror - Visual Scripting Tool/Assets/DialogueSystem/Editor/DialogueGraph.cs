using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

public class DialogueGraph : EditorWindow
{
    private DialogueGraphView _graphView;
    private string _fileName = "New Narrative";

    [MenuItem("Dialogue Editor/Dialogue Graph Editor")]
    public static void OpenDialogueEditorWindow()
    {
        DialogueGraph window = GetWindow<DialogueGraph>();
        window.titleContent = new GUIContent("Dialogue Graph Editor");
    }

    private void OnEnable()
    {
        ConstructGraphView();
        GenerateToolbar();
        GenerateMiniMap();
        GenerateBlackBoard();
    }
    
    private void ConstructGraphView()
    {
        _graphView = new DialogueGraphView(this)
        {
            name = "Dialogue Graph"
        };

        _graphView.StretchToParentSize();
        rootVisualElement.Add(_graphView);
    }

    /// <summary>
    /// Generates Minimap
    /// </summary>
    private void GenerateMiniMap()
    {
        MiniMap minimap = new MiniMap
        {
            anchored = true
        };
        
        //Set Minimap to Corner right with an offset of 10 px
        Vector2 coords = _graphView.contentViewContainer.WorldToLocal(new Vector2(maxSize.x - 10, 30));

        minimap.SetPosition(new Rect(coords.x, coords.y, 200, 140));
        _graphView.Add(minimap);
    }

    private void GenerateBlackBoard()
    {
        Blackboard blackboard = new Blackboard(_graphView)
        {
            //Event Called when + button is pressed
            addItemRequested = delegate(Blackboard _blackboard)
            {
                _graphView.AddPropertyToBlackBoard(new ExposedProperty());
            },
            
            //Event called when property is edited
            editTextRequested = delegate(Blackboard blackboard1, VisualElement element, string newValue)
            {
                string oldPropertyName = ((BlackboardField) element).text; // PropertyName
                
                // if Property name exists display error dialogue
                if (_graphView.ExposedProperties.Any(x => x.PropertyName == newValue))
                {
                    EditorUtility.DisplayDialog("Error",
                        "This Property name already exists, please choose another one!", "Sounds Good");
                    return;
                }

                int propertyIndex = _graphView.ExposedProperties.FindIndex(x => x.PropertyName == oldPropertyName);
                _graphView.ExposedProperties[propertyIndex].PropertyName = newValue;
                ((BlackboardField) element).text = newValue;
            }
        };
        
        blackboard.Add(new BlackboardSection
        {
            title = "Exposed Properties",
        });
        
        blackboard.SetPosition(new Rect(10,30,200,300));
        _graphView.Add(blackboard);
        _graphView.Blackboard = blackboard;
    }

    /// <summary>
    /// Creates the Menu Toolbar associated with the 
    /// GraphView and adds the appropriate items
    /// </summary>
    private void GenerateToolbar()
    {
        Toolbar toolbar = new Toolbar();

        ///Create Toolbar Menu
        ToolbarMenu menu = new ToolbarMenu
        {
            text = "Additional Actions",
            variant = ToolbarMenu.Variant.Popup
        };

        //Add elements to menu
        menu.menu.AppendAction("Debug Message", action => { Debug.Log("Debug Action"); }, action => DropdownMenuAction.Status.Normal);

        toolbar.Add(menu);

        //Label for FileName
        Label fileNameLabel = new Label()
        {
            style = {color = Color.black, unityTextAlign = TextAnchor.MiddleCenter, unityFontStyleAndWeight = FontStyle.Bold},
            text = "File Name"
        };

        toolbar.Add(fileNameLabel);

        /// FileName Textfield
        TextField fileName = new TextField();

        fileName.SetValueWithoutNotify(_fileName);
        fileName.MarkDirtyRepaint(); //Sets the textfield with GUI notification
        fileName.RegisterValueChangedCallback(callback => _fileName = callback.newValue);
        toolbar.Add(fileName);

        //Button for NodecCreation
        // ToolbarButton nodeCreationButton = new ToolbarButton(delegate
        // {
        //     _graphView.CreateNode("Dialogue Node");
        // });
        //
        // nodeCreationButton.tooltip = "Create a new Dialogue Node";
        // nodeCreationButton.text = "Create Node";
        // toolbar.Add(nodeCreationButton);

        //Button for Saving The Graph
        ToolbarButton saveGraphButton = new ToolbarButton(delegate
        {
            SaveDialogueGraph();
        });

        saveGraphButton.tooltip = "Save the Dialogue Graph";
        saveGraphButton.text = "Save Graph";
        toolbar.Add(saveGraphButton);

        //Button for Loading the Graph
        ToolbarButton loadGraphButton = new ToolbarButton(delegate
        {
            LoadDialogueGraph();
        });

        loadGraphButton.tooltip = "Load the Dialogue Graph";
        loadGraphButton.text = "Load Graph";
        toolbar.Add(loadGraphButton);

        rootVisualElement.Add(toolbar);
    }

    /// <summary>
    /// Calls saving Utitliy
    /// </summary>
    private void SaveDialogueGraph()
    {
        RequestDataOperation(true);
    }

    /// <summary>
    /// Loads saving Utility
    /// </summary>
    private void LoadDialogueGraph()
    {
        RequestDataOperation(false);
    }

    private void RequestDataOperation(bool toSave)
    {
        if (string.IsNullOrEmpty(_fileName))
        {
            EditorUtility.DisplayDialog("Invalid file name", "Please enter a valid file name", "Sound Good");
            return;
        }

        GraphSaveUtility saveUtility = GraphSaveUtility.GetInstance(_graphView);

        if (toSave)
        {
            saveUtility.SaveGraph(_fileName);
        }
        else
        {
            saveUtility.LoadGraph(_fileName);
        }
    }

    private void OnDisable()
    {
        rootVisualElement.Remove(_graphView);
    }

    #region HelperFunctions
    private ToolbarButton CreateToolbarButton(System.Action action, string tooltip, string name)
    {
        ToolbarButton toolbarButton = new ToolbarButton(action);

        toolbarButton.tooltip = tooltip;
        toolbarButton.name = name;
        return toolbarButton;
    }
    #endregion
}
