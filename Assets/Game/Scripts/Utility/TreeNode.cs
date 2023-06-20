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
    /// q‚ğİ’è‚µ‚Ü‚·
    /// </summary>
    /// <param name="child">İ’è‚·‚éq</param>
    public void AddChild(TreeNode<T> child)
    {
        Children.Add(child);
        child.Parent = this;
    }
}