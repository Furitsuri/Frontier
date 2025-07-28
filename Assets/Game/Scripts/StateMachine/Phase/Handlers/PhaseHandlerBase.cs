using Frontier.Stage;
using Frontier.Battle;
using Zenject;
using System.Collections;

namespace Frontier
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

        virtual public void Init()
        {
            // 遷移木の作成
            CreateTree();

            CurrentNode.Init();

            _isFirstUpdate = true;
        }

        virtual public bool Update()
        {
            if (_isInitReserved)
            {
                CurrentNode.RunState();
                _isInitReserved = false;
            }

            // 現在実行中のステートを更新
            if (CurrentNode.Update())
            {
                if (CurrentNode.IsBack() && CurrentNode.Parent == null)
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
                CurrentNode = CurrentNode.GetChildren<PhaseStateBase>( transitIndex );
                _isInitReserved = true; // 初期化を予約します
            }
            else if (CurrentNode.IsBack())
            {
                CurrentNode.ExitState();
                CurrentNode = CurrentNode.GetParent<PhaseStateBase>();
                _isInitReserved = true;
            }
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