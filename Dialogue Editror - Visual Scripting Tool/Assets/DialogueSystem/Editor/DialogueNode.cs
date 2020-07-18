﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.GraphView;

public class DialogueNode : Node
{
    //used as an indication for different nodes
    public string GUID;
    public string DialogueText;
    public bool EntryPoint = false;
}
