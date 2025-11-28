using Frontier.DebugTools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class StateBase : TreeNode<StateBase>
{
    [Inject] protected InputFacade _inputFcd = null;

    public bool IsExitReserved { get; private set; } = false;
    private int _transitIndex = -1;
    protected bool _isBack = false;

    public int TransitIndex => _transitIndex;

    /// <summary>
    /// 初期化します
    /// </summary>
    virtual public void Init()
    {
        IsExitReserved  = false;
        _transitIndex   = -1;
        _isBack         = false;
    }

    /// <summary>
    /// 親のステートノードを取得します
    /// </summary>
    /// <returns>親のステートノード</returns>
    public T GetParent<T>() where T : StateBase
    {
        return Parent as T;
    }

    /// <summary>
    /// 指定インデックスの子のステートノードを取得します
    /// </summary>
    /// <param name="index">指定するインデックス</param>
    /// <returns>子のステートノード</returns>
    public T GetChildren<T>(int index) where T : StateBase
    {
        if (index < 0 || index >= Children.Count)
        {
            Debug.LogError($"Index {index} is out of bounds for Children list of size {Children.Count}");
            return null;
        }

        var retChildren = Children[index] as T;

        if (retChildren == null)
        {
            Debug.LogError($"Children at index {index} is not of type {typeof(T).Name}");
            return null;
        }

        return retChildren;
    }

    /// <summary>
    /// 子のステートノードの列挙を取得します
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public IEnumerable<T> GetChildNodeEnumerable<T>() where T : StateBase
    {
        foreach (var child in Children)
        {
            yield return child as T;
        }
    }

    /// <summary>
    /// 更新します
    /// </summary>
    /// <returns>trueである場合は直前のフラグへ遷移</returns>
    virtual public bool Update()
    {
        return IsBack();
    }

    /// <summary>
    /// 現在のステートを実行します
    /// </summary>
    virtual public void RunState()
    {
        Init(); // ステートの初期化を行う
    }

    /// <summary>
    /// 現在のステートを再開します
    /// </summary>
    virtual public void RestartState()
    {
        _transitIndex = -1;
    }

    /// <summary>
    /// 現在のステートを中断します
    /// </summary>
    virtual public void PauseState() { }

    /// <summary>
    /// 現在のステートから退避します
    /// </summary>
    virtual public void ExitState() { }

    /// <summary>
    /// 以前のステートに戻るフラグを取得します
    /// </summary>
    /// <returns>戻るフラグ</returns>
    virtual public bool IsBack()
    {
        return _isBack;
    }

    protected void TransitState( int transitIdx )
    {
        _transitIndex = transitIdx;
    }

    protected void TransitStateWithExit( int transitIdx )
    {
        TransitState( transitIdx );

        IsExitReserved = true;
    }

    /// <summary>
    /// 親の遷移に戻ります
    /// </summary>
    protected void Back()
    {
        _isBack = true;
    }
}