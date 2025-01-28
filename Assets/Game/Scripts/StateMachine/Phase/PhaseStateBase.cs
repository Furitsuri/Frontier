using Frontier.Stage;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Frontier
{
    public class PhaseStateBase : TreeNode<PhaseStateBase>
    {
        private bool _isBack = false;
        public int TransitIndex { get; protected set; } = -1;
        protected HierarchyBuilder _hierarchyBld    = null;
        protected BattleManager _btlMgr             = null;
        protected StageController _stageCtrl        = null;

        [Inject]
        public void Construct( HierarchyBuilder hierarchyBld, BattleManager btlMgr, StageController stgCtrl)
        {
            _hierarchyBld   = hierarchyBld;
            _btlMgr         = btlMgr;
            _stageCtrl      = stgCtrl;
        }

        // ������
        virtual public void Init()
        {
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
        virtual public void UpdateInputGuide()
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
            _btlMgr.BtlCharaCdr.SetDiedCharacterKey(characterKey);
        }

        /// <summary>
        /// �K�C�h��V���ɒǉ����܂�
        /// </summary>
        /// <param name="addGuide">�ǉ�����K�C�h</param>
        protected void AddInputGuide(InputGuideUI.InputGuide addGuide )
        {

        }

        /// <summary>
        /// �X�e�[�g�̑J�ڂɕ����ăL�[�K�C�h��ύX���܂�
        /// </summary>
        /// <param name="keyGuideList">�J�ڐ�̃L�[�K�C�h���X�g</param>
        protected void SetInputGuides( List<InputGuideUI.InputGuide> keyGuideList )
        {
           //  GeneralUISystem.Instance.SetInputGuideList( keyGuideList );
        }

        /// <summary>
        /// �X�e�[�g�̑J�ڂɕ����ăL�[�K�C�h��ύX���܂�
        /// </summary>
        /// <param name="args">�J�ڐ�ŕ\������L�[�K�C�h�Q</param>
        protected void SetInputGuides(params (Constants.KeyIcon, string)[] args)
        {
            List<InputGuideUI.InputGuide> keyGuideList = new List<InputGuideUI.InputGuide>();
            foreach(var arg in args ){
                keyGuideList.Add(new InputGuideUI.InputGuide(arg));
            }

            // GeneralUISystem.Instance.SetInputGuideList(keyGuideList);
        }

        /// <summary>
        /// TODO : �S�ăR�[���o�b�N��o�^����`�ŃL�[�̎�t���o���Ȃ����̎����p
        /// </summary>
        /// <param name="args"></param>
        protected void SetInputGuides(params (Constants.KeyIcon, string, InputGuideUI.InputGuide.InputCallBack)[] args)
        {
            List<InputGuideUI.InputGuide> keyGuideList = new List<InputGuideUI.InputGuide>();
            foreach (var arg in args)
            {
                keyGuideList.Add(new InputGuideUI.InputGuide(arg));
            }

            // GeneralUISystem.Instance.SetInputGuideList(keyGuideList);
        }
    }
}