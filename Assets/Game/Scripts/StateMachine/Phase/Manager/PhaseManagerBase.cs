using Frontier.Stage;

namespace Frontier
{
    public class PhaseManagerBase : Tree<PhaseStateBase>
    {
        protected bool _isFirstUpdate = false;
        protected BattleManager _btlMgr = null;
        protected StageController _stageCtrl = null;

        virtual public void Init()
        {
            _btlMgr = ManagerProvider.Instance.GetService<BattleManager>();
            _stageCtrl = ManagerProvider.Instance.GetService<StageController>();

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