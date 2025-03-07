using Frontier.Stage;
using Frontier.Battle;
using Zenject;

namespace Frontier
{
    public class PhaseManagerBase : Tree<PhaseStateBase>
    {
        protected bool _isFirstUpdate               = false;
        protected HierarchyBuilder _hierarchyBld    = null;
        protected BattleRoutineController _btlRtnCtrl             = null;
        protected StageController _stgCtrl          = null;
        protected BattleUISystem _btlUi             = null;
        
        [Inject]
        public void Construct( HierarchyBuilder hierarchyBld, BattleRoutineController btlRtnCtrl, BattleUISystem btlUi, StageController stgCtrl)
        {
            _hierarchyBld   = hierarchyBld;
            _btlRtnCtrl         = btlRtnCtrl;
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
                CurrentNode.Init();
            }
            else if (CurrentNode.IsBack())
            {
                CurrentNode.Exit();
                CurrentNode = CurrentNode.Parent;
                CurrentNode.Init();
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