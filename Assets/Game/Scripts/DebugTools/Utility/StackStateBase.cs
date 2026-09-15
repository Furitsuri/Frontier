using System.Collections.Generic;
using Zenject;

#if UNITY_EDITOR

/// <summary>
/// StateBase(TreeNode.Parentという単一参照による戻り先管理)の代替となる基底クラス。
/// 戻り先の管理をノード自身の`Parent`フィールドではなく、Handler側が実行時に持つスタックの
/// Push/Popに委ねる。これにより、同一インスタンスを複数の親から子として登録しても
/// (AddChildを複数回呼んでも)、戻り先が上書きされて破綻することがない。
///
/// Children/AddChildは「どんな子Stateが存在するか」を宣言的に登録し、TransitState(index)の
/// 遷移先探索に使うだけの役割に限定しており、戻り先の管理には一切関与しない
/// (この点がStateBase/TreeNodeとの決定的な違い)。そのため「戻る以外の目的で親を辿って
/// ドメインロジックを呼び出す」ことは意図的にできない設計にしてある。そのような用途が
/// 必要な場合は、SetSendTransitionContext()で渡すコンテキストにコールバック(Action)自体を
/// 含める形にすること(呼び出し元を型で決め打ちして辿るのではなく、呼び出し元が自分から
/// 「戻ってきたら何をするか」を渡す形)。
///
/// 影響範囲をリスクの低いデバッグ用StageEditor機能(StackPhaseStateBase以下)に限定するための
/// 実験的な実装であり、本番コード(StateBase/PhaseStateBase側)には一切影響しない。
/// なお、このクラス自体はデバッグ機能に限定される内容を含まない汎用的な基底クラスである。
/// </summary>
public abstract class StackStateBase
{
    [Inject] protected InputFacade _inputFcd = null;

    private readonly List<StackStateBase> _children = new List<StackStateBase>();

    public bool IsExitReserved { get; private set; } = false;
    protected bool _isBack                = false;
    private object _sendTransitionContext = null;
    private int _transitIndex             = -1;

    public int TransitIndex => _transitIndex;

    /// <summary>
    /// 子Stateを登録します。戻り先の管理には使われません(TransitState(index)での遷移先探索、
    /// 及び構造をコード上で見渡せるようにするための宣言的な登録に留まる)。そのため、同じ子を
    /// 複数の親から登録しても問題ありません。
    /// </summary>
    public void AddChild( StackStateBase child )
    {
        _children.Add( child );
    }

    /// <summary>
    /// 指定インデックスの子Stateノードを取得します
    /// </summary>
    public T GetChildren<T>( int index ) where T : StackStateBase
    {
        if( index < 0 || index >= _children.Count )
        {
            UnityEngine.Debug.LogError( $"Index {index} is out of bounds for Children list of size {_children.Count}" );
            return null;
        }

        var child = _children[index] as T;
        if( child == null )
        {
            UnityEngine.Debug.LogError( $"Children at index {index} is not of type {typeof( T ).Name}" );
        }

        return child;
    }

    /// <summary>
    /// 子Stateノードの列挙を取得します
    /// </summary>
    public IEnumerable<T> GetChildNodeEnumerable<T>() where T : StackStateBase
    {
        foreach( var child in _children )
        {
            yield return child as T;
        }
    }

    /// <summary>
    /// 初期化します
    /// </summary>
    virtual public void Init( object context )
    {
        IsExitReserved         = false;
        _isBack                = false;
        _sendTransitionContext = null;
        _transitIndex          = -1;
    }

    public void SetSendTransitionContext( object transitionContext )
    {
        _sendTransitionContext = transitionContext;
    }

    public void ReceiveContext<T>( ref T receiveValue, object context )
    {
        if( context is T typed )
        {
            receiveValue = typed;
        }
    }

    virtual public bool Update() { return IsBack(); }

    virtual public void LateUpdate() { }

    virtual public void FixedUpdate() { }

    /// <summary>
    /// 現在のステートを実行します
    /// </summary>
    virtual public void OnEnter( object context )
    {
        Init( context );
        OnActivated();
    }

    /// <summary>
    /// 現在のステートを再開します
    /// </summary>
    virtual public void RestartState()
    {
        _transitIndex = -1;
        OnActivated();
    }

    /// <summary>
    /// 現在のステートを中断します
    /// </summary>
    virtual public object PauseState() { return _sendTransitionContext; }

    /// <summary>
    /// 現在のステートから退避します
    /// </summary>
    virtual public object ExitState() { return _sendTransitionContext; }

    /// <summary>
    /// 以前のステートに戻るフラグを取得します
    /// </summary>
    virtual public bool IsBack() { return _isBack; }

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
    /// ステートが起動した際に行われる処理です。継承先で定義してください。
    /// </summary>
    virtual protected void OnActivated()
    {
        _isBack = false;
    }

    /// <summary>
    /// 親の遷移に戻ります(実際の戻り先の解決はEditorHandlerBase側のスタックが行う)
    /// </summary>
    protected void Back()
    {
        _isBack = true;
    }
}

#endif // UNITY_EDITOR
