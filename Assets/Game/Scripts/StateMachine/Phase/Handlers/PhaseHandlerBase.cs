using Frontier.Battle;
using Frontier.Stage;
using Frontier.UI;
using System.Collections;
using Zenject;

namespace Frontier.StateMachine
{
    public class PhaseHandlerBase : Tree<PhaseStateBase>
    {
        [Inject] protected IUiSystem _uiSystem                  = null;
        [Inject] protected HierarchyBuilderBase _hierarchyBld   = null;
        [Inject] protected BattleRoutineController _btlRtnCtrl  = null;
        [Inject] protected StageController _stgCtrl             = null;
        [Inject] protected BattleUISystem _btlUi                = null;

        private object _transitionContext;    // State間で受け渡すコンテキスト情報
        protected bool _isFirstUpdate = false;

        public void ReceiveContext( object context )
        {
            _transitionContext = context;
        }

        public void SendContext( out object receieve )
        {
            receieve = _transitionContext;
            _transitionContext = null;   // 受け渡し後はクリア
        }

        private void AssignHandler( PhaseStateBase state )
        {
            state.AssignHandler( this );
        }

        /// <summary>
        /// 初期化します
        /// MEMO : CurrentNode::Init()は、CurrentNode::RunState()が呼ばれた際にセットとして自動で呼び出されるため、このタイミングでは呼びません。
        /// </summary>
        virtual public void Init()
        {
            CreateTree();   // 遷移木の作成

            Traverse( RootNode, AssignHandler );    // 各ステートにハンドラを割り当てる

            _isFirstUpdate = true;
        }

        virtual public void Update()
        {
            CurrentNode.Update();   // 現在実行中のステートを更新
        }

        virtual public bool LateUpdate()
        {
            // ステートの遷移を監視
            int transitIndex = CurrentNode.TransitIndex;
            if( 0 <= transitIndex )
            {
                if( CurrentNode.IsExitReserved ) { CurrentNode.ExitState(); }   // 終了
                else { CurrentNode.PauseState(); }                              // 中断

                CurrentNode = CurrentNode.GetChildren<PhaseStateBase>( transitIndex );
                CurrentNode.RunState();
            }
            else if( CurrentNode.IsBack() )
            {
                // CurrentNodeがフェーズ終了通知を発行しているか、
                // 親がフェーズアニメーションステート、または親が存在しない場合はフェーズ遷移完了とみなす
                if( CurrentNode.IsEndedPhase || null == CurrentNode.Parent || CurrentNode.Parent is PhaseAnimationStateBase )
                {
                    CurrentNode.ExitState();
                    return true;
                }

                CurrentNode.ExitState();
                CurrentNode = CurrentNode.GetParent<PhaseStateBase>();

                // Exit処理が行われたノードはRunを実行。それ以外はPause処理が行われたためRestartを実行
                if( CurrentNode.IsExitReserved ) { CurrentNode.RunState(); }
                else { CurrentNode.RestartState(); }
            }

            return false;
        }

        virtual public void Run()
        {
            Init();
            CurrentNode.RunState();     // ステートの開始
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

                CurrentNode = CurrentNode.GetParent<PhaseStateBase>();
            }
        }
    }
}