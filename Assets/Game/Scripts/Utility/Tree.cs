using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tree<T> where T : TreeNode<T>
{
    public T RootNode { get; protected set; }
    public T CurrentNode { get; protected set; }

    public Tree()
    {
        RootNode = null;
        CurrentNode = null;
    }

    virtual protected void CreateTree() { }
}
