using Frontier.Battle;
using Frontier.Stage;
using Frontier.UI;
using System.Collections;
using Zenject;

namespace Frontier.StateMachine
{
    public class PhaseHandlerBase : Tree<PhaseStateBase>
    {
        protected bool _isInitReserved = false;
        protected bool _isFirstUpdate = false;
        protected HierarchyBuilderBase _hierarchyBld = null;
        protected BattleRoutineController _btlRtnCtrl = null;
        protected StageController _stgCtrl = null;
        protected BattleUISystem _btlUi = null;
        [Inject] protected IUiSystem _uiSystem = null;

        [Inject]
        public void Construct( HierarchyBuilderBase hierarchyBld, BattleRoutineController btlRtnCtrl, BattleUISystem btlUi, StageController stgCtrl )
        {
            _hierarchyBld = hierarchyBld;
            _btlRtnCtrl = btlRtnCtrl;
            _btlUi = btlUi;
            _stgCtrl = stgCtrl;
        }

        /// <summary>
        /// 初期化します
        /// MEMO : CurrentNode::Init()は、CurrentNode::RunState()が呼ばれた際にセットとして自動で呼び出されるため、このタイミングでは呼びません。
        /// </summary>
        virtual public void Init()
        {
            // 遷移木の作成
            CreateTree();

            _isFirstUpdate = true;
        }

        virtual public void Update()
        {
            /*
            if( _isInitReserved )
            {
                CurrentNode.RunState();
                _isInitReserved = false;
            }
            else
            {
                CurrentNode.RestartState();
            }
            */

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
                // _isInitReserved = true; // 初期化を予約します
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
                // if( CurrentNode.IsExitReserved ) { _isInitReserved = true; } // 以前のノードがExitしていた場合は初期化
                if( CurrentNode.IsExitReserved )
                {
                    CurrentNode.RunState();
                }
                else
                {
                    CurrentNode.RestartState();
                }
            }

            return false;
        }

        virtual public void Run()
        {
            // ステートの開始
            Init();
            CurrentNode.RunState();
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

                CurrentNode = CurrentNode.GetParent<PhaseStateBase>();
            }
        }
    }
}