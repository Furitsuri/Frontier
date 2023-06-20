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
    /// 子を設定します
    /// </summary>
    /// <param name="child">設定する子</param>
    public void AddChild(TreeNode<T> child)
    {
        Children.Add(child);
        child.Parent = this;
    }
}