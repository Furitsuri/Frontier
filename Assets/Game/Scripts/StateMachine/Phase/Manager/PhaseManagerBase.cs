using Frontier.Stage;

namespace Frontier
{
    public class PhaseManagerBase : Tree<PhaseStateBase>
    {
        protected bool _isFirstUpdate = false;
        protected BattleManager _btlMgr = null;
        protected StageController _stageCtrl = null;

        virtual public void Regist(BattleManager btlMgr, StageController stgCtrl)
        {
            _btlMgr = btlMgr;
            _stageCtrl = stgCtrl;
        }

        virtual public void Init()
        {
            // �J�ږ؂̍쐬
            CreateTree();

            CurrentNode.Init(_btlMgr,_stageCtrl);

            _isFirstUpdate = true;
        }

        virtual public bool Update()
        {
            // ���ݎ��s���̃X�e�[�g���X�V
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
            // �X�e�[�g�̑J�ڂ��Ď�
            int transitIndex = CurrentNode.TransitIndex;
            if (0 <= transitIndex)
            {
                CurrentNode.Exit();
                CurrentNode = CurrentNode.Children[transitIndex];
                CurrentNode.Init(_btlMgr,_stageCtrl);
            }
            else if (CurrentNode.IsBack())
            {
                CurrentNode.Exit();
                 CurrentNode = CurrentNode.Parent;
                CurrentNode.Init(_btlMgr, _stageCtrl);
            }
        }

        /// <summary>
        /// �t�F�[�Y�A�j���[�V�������Đ����܂�
        /// </summary>
        virtual protected void StartPhaseAnim()
        {
        }
    }
}