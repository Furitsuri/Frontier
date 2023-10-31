using Frontier.Stage;
using UnityEngine;

namespace Frontier
{
    public class PhaseStateBase : TreeNode<PhaseStateBase>
    {
        private bool _isBack = false;
        public int TransitIndex { get; protected set; } = -1;
        protected BattleManager _btlMgr = null;
        protected StageController _stageCtrl = null;

        // ������
        virtual public void Init(BattleManager btlMgr, StageController stgCtrl)
        {
            _btlMgr         = btlMgr;
            _stageCtrl      = stgCtrl;
            TransitIndex    = -1;
            _isBack         = false;
        }

        // �X�V
        virtual public bool Update()
        {
            if (Input.GetKeyUp(KeyCode.Backspace))
            {
                Back();

                return true;
            }

            return false;
        }

        // �ޔ�
        virtual public void Exit()
        {
        }

        // �߂�
        virtual public bool IsBack()
        {
            return _isBack;
        }

        /// <summary>
        /// �e�̑J�ڂɖ߂�܂�
        /// </summary>
        protected void Back()
        {
            _isBack = true;
        }

        protected void NoticeCharacterDied(CharacterHashtable.Key characterKey)
        {
            _btlMgr.SetDiedCharacterKey(characterKey);
        }
    }
}