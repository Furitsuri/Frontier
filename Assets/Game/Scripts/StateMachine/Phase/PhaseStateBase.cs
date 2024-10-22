using Frontier.Stage;
using System.Collections.Generic;
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

        /// <summary>
        /// �L�[�K�C�h���X�V���܂�
        /// </summary>
        virtual public void UpdateKeyGuide()
        {

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

        /// <summary>
        /// ���S�����L�����N�^�[�̑��݂�ʒm���܂�
        /// </summary>
        /// <param name="characterKey">���S�����L�����N�^�[�̃n�b�V���L�[</param>
        protected void NoticeCharacterDied(CharacterHashtable.Key characterKey)
        {
            _btlMgr.SetDiedCharacterKey(characterKey);
        }

        /// <summary>
        /// �K�C�h��V���ɒǉ����܂�
        /// </summary>
        /// <param name="addGuide"></param>
        protected void AddKeyGuide(KeyGuideUI.KeyGuide addGuide )
        {

        }

        /// <summary>
        /// �X�e�[�g�̑J�ڂɕ����ăL�[�K�C�h��ύX���܂�
        /// </summary>
        /// <param name="keyGuideList">�J�ڐ�̃L�[�K�C�h���X�g</param>
        protected void TransitKeyGuides( List<KeyGuideUI.KeyGuide> keyGuideList )
        {
            GeneralUISystem.Instance.TransitKeyGuide( keyGuideList );
        }

        /// <summary>
        /// �X�e�[�g�̑J�ڂɕ����ăL�[�K�C�h��ύX���܂�
        /// </summary>
        /// <param name="args">�J�ڐ�ŕ\������L�[�K�C�h�Q</param>
        protected void TransitKeyGuides(params (Constants.KeyIcon, string)[] args)
        {
            List<KeyGuideUI.KeyGuide> keyGuideList = new List<KeyGuideUI.KeyGuide>();
            foreach(var arg in args ){
                keyGuideList.Add(new KeyGuideUI.KeyGuide(arg));
            }

            GeneralUISystem.Instance.TransitKeyGuide(keyGuideList);
        }
    }
}