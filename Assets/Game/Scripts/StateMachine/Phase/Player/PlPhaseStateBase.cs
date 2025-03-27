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

        override public void Init()
        {
            base.Init();

            _selectPlayer = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter() as Player;
            // MEMO : �p����̃N���X�ɂ���Ă�null�����e���邽��null�`�F�b�N�͍s��Ȃ�
        }

        /// <summary>
        /// �L�����Z�����͂��󂯂��ۂ̏������s���܂�
        /// </summary>
        /// <param name="isCancel">�L�����Z������</param>
        /// <returns>���͎��s�̗L��</returns>
        override protected bool AcceptCancel( bool isCancel )
        {
            if ( !isCancel ) return false;

            Back();

            return true;
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