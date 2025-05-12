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
        protected HierarchyBuilder _hierarchyBld        = null;
        protected BattleRoutineController _btlRtnCtrl   = null;
        protected StageController _stgCtrl              = null;
        protected BattleUISystem _btlUi                 = null;
        
        [Inject]
        public void Construct( HierarchyBuilder hierarchyBld, BattleRoutineController btlRtnCtrl, BattleUISystem btlUi, StageController stgCtrl)
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
                CurrentNode.Init();
                _isInitReserved = false;
            }

            // 現在実行中のステートを更新
            if (CurrentNode.Update())
            {
                if (CurrentNode.IsBack() && CurrentNode.Parent == null)
                {
                    CurrentNode.Exit();

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
                CurrentNode.Exit();
                CurrentNode = CurrentNode.Children[transitIndex];
                _isInitReserved = true;
            }
            else if (CurrentNode.IsBack())
            {
                CurrentNode.Exit();
                CurrentNode = CurrentNode.Parent;
                _isInitReserved = true;
            }
        }

        /// <summary>
        /// フェーズアニメーションを再生します
        /// </summary>
        virtual protected void StartPhaseAnim()
        {
        }
    }
}