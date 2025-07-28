using System.Collections;
using System.Collections.Generic;
using System.Linq;

public abstract class TreeNode
{
    public TreeNode Parent { get; protected set; }
    public List<TreeNode> Children { get; } = new();
}

public class TreeNode<T> : TreeNode where T : TreeNode<T>
{
    public new T Parent
    {
        get => base.Parent as T;
        protected set => base.Parent = value;
    }

    public new List<T> Children => base.Children.Cast<T>().ToList();

    public void AddChild(T child)
    {
        base.Children.Add(child);
        child.Parent = this as T;
    }
}