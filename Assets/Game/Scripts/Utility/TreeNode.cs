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
    /// �q��ݒ肵�܂�
    /// </summary>
    /// <param name="child">�ݒ肷��q</param>
    public void AddChild(T child)
    {
        Children.Add(child);
        child.Parent = this as T;
    }
}