using Frontier.Battle;
using Frontier.FormTroop;
using Frontier.Stage;
using Frontier.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using Zenject;

namespace Frontier.StateMachine
{
    /// <summary>
    /// PhaseStateBase(StackStateBase)の木を実行するHandler基底クラス。
    /// 戻り先の管理は、各ノードの`Parent`参照ではなく、このクラスが実行時に保持するスタック
    /// (`_returnStack`)へのPush/Popで行う。TransitState()で子へ遷移する直前に現在のノードを
    /// スタックへpushし、Back()で戻る際にpopして戻り先を決定する。この方式では、同一の子State
    /// インスタンスを複数の親からAddChildしても、戻り先が上書きされて破綻することがない
    /// (詳細はStackStateBaseのコメントを参照)。
    /// </summary>
    public class PhaseHandlerBase
    {
        public PhaseStateBase RootNode    { get; protected set; }
        public PhaseStateBase CurrentNode { get; protected set; }

        protected HierarchyBuilderBase _hierarchyBld    = null;
        protected bool _isFirstUpdate                   = false;

        // 遷移元ノードを記録するスタック。Back()で戻る際、GetParent<T>()の代わりにここからpopする
        private readonly Stack<PhaseStateBase> _returnStack = new Stack<PhaseStateBase>();

        [Inject] public PhaseHandlerBase( HierarchyBuilderBase hierarchyBld )
        {
            _hierarchyBld = hierarchyBld;

            CreateTree();                           // 遷移木の作成
            Traverse( RootNode, AssignHandler );    // 各ステートにハンドラを割り当てる
        }

        private void AssignHandler( PhaseStateBase state )
        {
            state.AssignHandler( this );
        }

        /// <summary>
        /// 指定ノードとその子孫すべてに対してactionを実行します(旧Tree&lt;T&gt;.Traverse()の代替)。
        /// </summary>
        private void Traverse( PhaseStateBase node, Action<PhaseStateBase> action )
        {
            if( null == node ) { return; }

            action( node );

            foreach( var child in node.GetChildNodeEnumerable<PhaseStateBase>() )
            {
                Traverse( child, action );
            }
        }

        protected void AssignPresenterToNodes( PhaseStateBase targetNode, PhasePresenterBase presenter )
        {
            if( null == targetNode ) { return; }

            targetNode.AssignPresenter( presenter );

            foreach( var childNode in targetNode.GetChildNodeEnumerable<PhaseStateBase>() )
            {
                AssignPresenterToNodes( childNode, presenter );
            }
        }

        /// <summary>
        /// 初期化します
        /// MEMO : CurrentNode::Init()は、CurrentNode::RunState()が呼ばれた際にセットとして自動で呼び出されるため、このタイミングでは呼びません。
        /// </summary>
        virtual public void Init()
        {
            CurrentNode     = RootNode;
            _isFirstUpdate  = true;

            _returnStack.Clear();
        }

        virtual public void Update()
        {
            CurrentNode.Update();   // 現在実行中のステートを更新
        }

        virtual public bool LateUpdate()
        {
            object transitionContext = null;

            CurrentNode.LateUpdate();  // 現在実行中のステートを後更新

            // ステートの遷移を監視
            int transitIndex = CurrentNode.TransitIndex;
            if( 0 <= transitIndex )
            {
                if( CurrentNode.IsExitReserved ) { transitionContext = CurrentNode.ExitState(); }   // 終了
                else { transitionContext = CurrentNode.PauseState(); }                              // 中断

                _returnStack.Push( CurrentNode );    // 戻り先としてスタックへ記録
                CurrentNode = CurrentNode.GetChildren<PhaseStateBase>( transitIndex );
                CurrentNode.OnEnter( transitionContext );
            }
            else if( CurrentNode.IsBack() )
            {
                // CurrentNodeがフェーズ終了通知を発行しているか、
                // 親がフェーズアニメーションステート、または親が存在しない場合はフェーズ遷移完了とみなす
                bool hasParent = 0 < _returnStack.Count;
                if( CurrentNode.IsEndedPhase || !hasParent || _returnStack.Peek() is PhaseAnimationStateBase )
                {
                    // CurrentNodeから根まで、すべての祖先のExitState()を呼ぶ。
                    // (CurrentNodeのExitState()だけを呼ぶと、親States(RootState等)が保持しているクリーンアップ処理・確定処理が呼ばれないまま終了してしまう)
                    var node = CurrentNode;
                    while( node != null )
                    {
                        node.ExitState();
                        node = 0 < _returnStack.Count ? _returnStack.Pop() : null;
                    }
                    return true;
                }

                transitionContext = CurrentNode.ExitState();
                CurrentNode = _returnStack.Pop();

                // Exit処理が行われたノードはRunを実行。それ以外はPause処理が行われたためRestartを実行
                if( CurrentNode.IsExitReserved ) { CurrentNode.OnEnter( transitionContext ); }
                else { CurrentNode.RestartState(); }
            }

            return false;
        }

        virtual public void FixedUpdate()
        {
            CurrentNode.FixedUpdate();  // 現在実行中のステートを固定更新
        }

        virtual public void Enter()
        {
            Init();
            CurrentNode.OnEnter( null );     // ステートの開始
        }

        virtual public void Restart()
        {
            CurrentNode.RestartState(); // ステートの再開
        }

        virtual public void Pause()
        {
            CurrentNode.PauseState();   // ステートの一時停止
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

                CurrentNode = 0 < _returnStack.Count ? _returnStack.Pop() : null;
            }
        }

        /// <summary>
        /// 遷移木を構築します。派生クラスでRootNode/CurrentNodeを設定してください。
        /// </summary>
        virtual protected void CreateTree() { }
    }
}