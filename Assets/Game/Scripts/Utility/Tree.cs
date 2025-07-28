using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tree<T> where T : TreeNode
{
    public T RootNode { get; protected set; }
    public T CurrentNode { get; protected set; }

    public Tree()
    {
        RootNode = null;
        CurrentNode = null;
    }

    public void Traverse(T node, Action<T> action)
    {
        if (node == null) return;
        action(node);
        foreach (var child in node.Children)
        {
            Traverse((T)child, action);
        }
    }

    virtual protected void CreateTree() { }
}