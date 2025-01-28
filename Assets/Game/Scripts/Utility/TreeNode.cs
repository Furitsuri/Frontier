using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeNode<T> where T : TreeNode<T>
{
    public T Parent { get; private set; }
    public List<T> Children { get; private set; }

    public TreeNode()
    {
        Parent = null;
        Children = new List<T>();
    }

    /// <summary>
    /// 子を設定します
    /// </summary>
    /// <param name="child">設定する子</param>
    public void AddChild(T child)
    {
        Children.Add(child);
        child.Parent = this as T;
    }
}