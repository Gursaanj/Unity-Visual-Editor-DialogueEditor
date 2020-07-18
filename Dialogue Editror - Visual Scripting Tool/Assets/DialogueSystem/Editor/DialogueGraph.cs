using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor;
public class DialogueGraph : EditorWindow
{
    private DialogueGraphView _graphView;


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
    }

    private void ConstructGraphView()
    {
        _graphView = new DialogueGraphView
        {
            name = "Dialogue Graph"
        };

        _graphView.StretchToParentSize();
        rootVisualElement.Add(_graphView);
    }

    private void GenerateToolbar()
    {
        Toolbar toolbar = new Toolbar();

        ToolbarButton nodeCreationButton = new ToolbarButton(delegate
        {
            _graphView.CreateNode("Dialogue Node");
        });

        nodeCreationButton.tooltip = "Create a new Dialogue Node";

        nodeCreationButton.text = "Create Node";
        toolbar.Add(nodeCreationButton);

        rootVisualElement.Add(toolbar);
    }

    private void OnDisable()
    {
        rootVisualElement.Remove(_graphView);
    }
}
