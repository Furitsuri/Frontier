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
        protected Player _selectPlayer = null;

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
        virtual protected bool DetectRevertInput()
        {
            if (_inputFcd.GetInputCancel())
            {
                Back();

                return true;
            }

            return false;
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