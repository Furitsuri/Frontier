using Frontier;
using Frontier.Battle;
using Frontier.Stage;
using Frontier.Entities;
using UnityEngine;
using Zenject;

namespace Frontier
{
    public class PlPhaseStateBase : PhaseStateBase
    {
        protected InputFacade _inputFcd = null;

        protected Player _selectPlayer = null;

        [Inject]
        public void Construct( HierarchyBuilder hierarchyBld, InputFacade inputFcd, BattleRoutineController btlRtnCtrl, StageController stgCtrl, UISystem uiSystem )
        {
            _hierarchyBld   = hierarchyBld;
            _inputFcd       = inputFcd;
            _btlRtnCtrl     = btlRtnCtrl;
            _stageCtrl      = stgCtrl;
            _uiSystem       = uiSystem;
        }

        /// <summary>
        /// ���݂̃X�e�[�g����ޔ����܂�
        /// </summary>
        override public void Exit()
        {
            _inputFcd.ResetInputCodes();
        }

        /// <summary>
        /// ���͂����m���āA�ȑO�̃X�e�[�g�ɑJ�ڂ���t���O��ON�ɐ؂�ւ��܂�
        /// </summary>
        virtual protected void DetectRevertInput()
        {
            if (_inputFcd.GetInputCancel())
            {
                Back();
            }
        }

        /// <summary>
        /// �ȑO�̏�ԂɊ����߂��܂�
        /// </summary>
        protected void Rewind()
        {
            if (_selectPlayer == null) return;

            _selectPlayer.RewindToPreviousState();
            _stageCtrl.ApplyCurrentGrid2CharacterGrid(_selectPlayer);
        }

        /// <summary>
        /// ���͂���t�邩���擾���܂�
        /// �����̃P�[�X�ł�����̊֐���p���Ĕ��肵�܂�
        /// </summary>
        /// <returns>���͎�t�̉�</returns>
        protected bool CanAcceptInputDefault()
        {
            // ���݂̃X�e�[�g����E�o����ꍇ�͓��͂��󂯕t���Ȃ�
            return !IsBack();
        }

    }
}