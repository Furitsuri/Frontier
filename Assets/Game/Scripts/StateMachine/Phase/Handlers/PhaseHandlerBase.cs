using Frontier.Stage;
using Frontier.Battle;
using Zenject;
using System.Collections;

namespace Frontier.StateMachine
{
    public class PhaseHandlerBase : Tree<PhaseStateBase>
    {
        protected bool _isInitReserved                  = false;
        protected bool _isFirstUpdate                   = false;
        protected HierarchyBuilderBase _hierarchyBld    = null;
        protected BattleRoutineController _btlRtnCtrl   = null;
        protected StageController _stgCtrl              = null;
        protected BattleUISystem _btlUi                 = null;
        
        [Inject]
        public void Construct( HierarchyBuilderBase hierarchyBld, BattleRoutineController btlRtnCtrl, BattleUISystem btlUi, StageController stgCtrl)
        {
            _hierarchyBld   = hierarchyBld;
            _btlRtnCtrl     = btlRtnCtrl;
            _btlUi          = btlUi;
            _stgCtrl        = stgCtrl;
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
            if (_isInitReserved)
            {
                CurrentNode.RunState();
                _isInitReserved = false;
            }

            CurrentNode.Update();   // 現在実行中のステートを更新
        }

        virtual public bool LateUpdate()
        {
            // ステートの遷移を監視
            int transitIndex = CurrentNode.TransitIndex;
            if( 0 <= transitIndex )
            {
                CurrentNode.ExitState();
                CurrentNode = CurrentNode.GetChildren<PhaseStateBase>( transitIndex );
                _isInitReserved = true; // 初期化を予約します
            }
            else if( CurrentNode.IsBack() )
            {
                CurrentNode.ExitState();

                if( null == CurrentNode.Parent || CurrentNode.Parent is PhaseAnimationStateBase ) { return true; }

                CurrentNode = CurrentNode.GetParent<PhaseStateBase>();
                _isInitReserved = true;
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
            // ステートの終了
            CurrentNode.ExitState();
        }

        /// <summary>
        /// フェーズアニメーションを再生します
        /// </summary>
        virtual protected void StartPhaseAnim()
        {
        }
    }
}