using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeNode<T> where T : TreeNode<T>
{
    private TreeNode<T> Parent { get; set; }
    private List<TreeNode<T>> Children { get; set; }

    public TreeNode()
    {
        Parent = null;
        Children = new List<TreeNode<T>>();
    }

    /// <summary>
    /// �q��ݒ肵�܂�
    /// </summary>
    /// <param name="child">�ݒ肷��q</param>
    public void AddChild(TreeNode<T> child)
    {
        Children.Add(child);
        child.Parent = this;
    }
}