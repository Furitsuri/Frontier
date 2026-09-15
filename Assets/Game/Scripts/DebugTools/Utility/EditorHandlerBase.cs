using Frontier.Battle;
using Frontier.Stage;
using Frontier.StateMachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

#if UNITY_EDITOR

/// <summary>
/// EditorStateBase(EditorStackStateBase)の木を実行するHandler基底クラス。
/// 戻り先の管理は、各ノードの`Parent`参照ではなく、このクラスが実行時に保持するスタック
/// (`_returnStack`)へのPush/Popで行う。TransitState()で子へ遷移する直前に現在のノードを
/// スタックへpushし、Back()で戻る際にpopして戻り先を決定する。この方式では、同一の子State
/// インスタンスを複数の親からAddChildしても、戻り先が上書きされて破綻することがない
/// (詳細はEditorStackStateBaseのコメントを参照)。
/// </summary>
public class EditorHandlerBase
{
    public EditorStateBase RootNode    { get; protected set; }
    public EditorStateBase CurrentNode { get; protected set; }

    [Inject] protected HierarchyBuilderBase _hierarchyBld = null;

    protected bool _isInitReserved      = false;
    private object _transitionContext   = null;    // State間で受け渡すコンテキスト情報

    // 遷移元ノードを記録するスタック。Back()で戻る際、GetParent<T>()の代わりにここからpopする
    private Stack<EditorStateBase> _returnStack = new Stack<EditorStateBase>();

    public void SetTransitionContext( object context )
    {
        _transitionContext = context;
    }

    virtual public void Init()
    {
        // 遷移木の作成
        CreateTree();

        CurrentNode.Init( _transitionContext );
        _transitionContext = null;
    }

    virtual public bool Update()
    {
        if (_isInitReserved)
        {
            CurrentNode.OnEnter( _transitionContext );
            _isInitReserved = false;
        }

        // 現在実行中のステートを更新
        if (CurrentNode.Update())
        {
            if (CurrentNode.IsBack() && _returnStack.Count == 0)
            {
                CurrentNode.ExitState();

                return true;
            }
        }

        return false;
    }

    virtual public void LateUpdate()
    {
        // ステートの遷移を監視
        int transitIndex = CurrentNode.TransitIndex;
        if (0 <= transitIndex)
        {
            CurrentNode.ExitState();
            _returnStack.Push( CurrentNode );    // 戻り先としてスタックへ記録
            CurrentNode = CurrentNode.GetChildren<EditorStateBase>(transitIndex);
            _isInitReserved = true;
        }
        else if (CurrentNode.IsBack())
        {
            CurrentNode.ExitState();
            CurrentNode = _returnStack.Count > 0 ? _returnStack.Pop() : null;
            _isInitReserved = true;
        }
    }

    virtual public void Enter()
    {
        // ステートの実行
        CurrentNode.OnEnter( _transitionContext );
    }

    virtual public void Restart()
    {
        // ステートの再開
        CurrentNode.RestartState();
    }

    virtual public void Pause()
    {
        // ステートの一時停止
        CurrentNode.PauseState();
    }

    virtual public void Exit()
    {
        while( CurrentNode != null )
        {
            // Exit処理が行われていなかったノードをすべて終了させる
            if( !CurrentNode.IsExitReserved )
            {
                CurrentNode.ExitState();
            }

            CurrentNode = _returnStack.Count > 0 ? _returnStack.Pop() : null;
        }
    }

    /// <summary>
    /// フェーズアニメーションを再生します
    /// </summary>
    virtual protected void StartPhaseAnim()
    {
    }

    /// <summary>
    /// 遷移木を構築します。派生クラスでRootNode/CurrentNodeを設定してください。
    /// </summary>
    virtual protected void CreateTree() { }
}

#endif // UNITY_EDITOR