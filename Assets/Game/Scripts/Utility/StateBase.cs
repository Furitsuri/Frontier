using Frontier.DebugTools;
using UnityEngine;
using Zenject;

public class StateBase : TreeNode<StateBase>
{
    public int TransitIndex { get; protected set; } = -1;

    protected bool _isBack = false;
    protected InputFacade _inputFcd = null;

    [Inject]
    public void Construct(InputFacade inputFcd)
    {
        _inputFcd = inputFcd;
    }

    /// <summary>
    /// 初期化します
    /// </summary>
    virtual public void Init()
    {
        TransitIndex = -1;
        _isBack = false;
    }

    /// <summary>
    /// 親のステートノードを取得します
    /// </summary>
    /// <returns>親のステートノード</returns>
    public T GetParent<T>() where T : StateBase
    {
        var retParent = Parent as T;

        if (retParent == null)
        {
            Debug.LogError($"Parent is not of type {typeof(T).Name}");
            return null;
        }

        return retParent;
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
        // ステートの初期化を行う
        Init();
        RegisterInputCodes();
    }

    /// <summary>
    /// 現在のステートを再開します
    /// </summary>
    virtual public void RestartState()
    {
        RegisterInputCodes();
    }

    /// <summary>
    /// 現在のステートを中断します
    /// </summary>
    virtual public void PauseState()
    {
        UnregisterInputCodes();
    }

    /// <summary>
    /// 現在のステートから退避します
    /// </summary>
    virtual public void ExitState()
    {
        UnregisterInputCodes();
    }

    /// <summary>
    /// 以前のステートに戻るフラグを取得します
    /// </summary>
    /// <returns>戻るフラグ</returns>
    virtual public bool IsBack()
    {
        return _isBack;
    }

    /// <summary>
    /// 親の遷移に戻ります
    /// </summary>
    protected void Back()
    {
        _isBack = true;
    }

    /// <summary>
    /// 入力コードを登録します
    /// </summary>
    virtual public void RegisterInputCodes() { }

    virtual public void UnregisterInputCodes()
    {
        _inputFcd.UnregisterInputCodes();
    }
}