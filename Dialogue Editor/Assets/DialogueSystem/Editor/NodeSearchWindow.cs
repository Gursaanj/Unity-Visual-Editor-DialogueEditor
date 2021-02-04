using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// SearchWindow needs to derive from ScriptableObject to be utilized as a dataProvider for SearchWindow.Open
/// </summary>
public class NodeSearchWindow : ScriptableObject, ISearchWindowProvider
{

    private DialogueGraphView _graphView;
    private EditorWindow _editorWindow;
    private Texture2D _indentationIcon;

    public void Init(EditorWindow window, DialogueGraphView view)
    {
        _graphView = view;
        _editorWindow = window;

        // Indentation Hack for search window SearchTreeEntry as a transparent icon
        _indentationIcon = new Texture2D(1, 1);
        _indentationIcon.SetPixel(0,0,new Color(0,0,0,0));
        _indentationIcon.Apply();
    }

    public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
    {
        List<SearchTreeEntry> tree = new List<SearchTreeEntry>
        {
            //First entry is the header (i.e. level 0)
            new SearchTreeGroupEntry(new GUIContent("Create Elements"), 0),
            new SearchTreeGroupEntry(new GUIContent("Dialogue", "Use this to create Dialogue related nodes"), 1),
            new SearchTreeEntry(new GUIContent("Dialogue Node", _indentationIcon,"Base Dialogue Node"))
            {
                level = 2,
                userData = new DialogueNode()
            }
            
        };

        return tree;
    }
    
    
    public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
    {
        if (_graphView == null || _editorWindow == null)
        {
            return false;
        }
        
        //Get Relative world position of search window in regards to the Editor Window visual element
        Vector2 worldMousePosition = _editorWindow.rootVisualElement.ChangeCoordinatesTo(
            _editorWindow.rootVisualElement.parent, context.screenMousePosition - _editorWindow.position.position);
        
        // Transform to local coordinates based on GraphView
        Vector2 localMousePosition = _graphView.contentViewContainer.WorldToLocal(worldMousePosition);
        
        
        switch (searchTreeEntry.userData)
        {
            case DialogueNode node:
            {
                Debug.Log("Created");
                _graphView.CreateNode("Dialogue Node", localMousePosition);
                return true;
            }
            default:
            {
                return false;
            }
        }
    }
}
