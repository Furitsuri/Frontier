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
        /// �L�����Z�����͂��󂯂��ۂ̏������s���܂�
        /// </summary>
        /// <param name="isRevert"></param>
        virtual protected bool AcceptRevertInput(bool isRevert)
        {
            if (!isRevert) return false;

            Back();

            return true;
        }

        /// <summary>
        /// �������͂��󂯎�����ۂ̏������s���܂�
        /// </summary>
        /// <param name="dir">��������</param>
        virtual protected bool AcceptDirectionInput(Constants.Direction dir)
        {
            return _stageCtrl.OperateGridCursor(dir);
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
    }
}