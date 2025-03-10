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
        /// 現在のステートから退避します
        /// </summary>
        override public void Exit()
        {
            _inputFcd.ResetInputCodes();
        }

        /// <summary>
        /// 入力を検知して、以前のステートに遷移するフラグをONに切り替えます
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
        /// 以前の状態に巻き戻します
        /// </summary>
        protected void Rewind()
        {
            if (_selectPlayer == null) return;

            _selectPlayer.RewindToPreviousState();
            _stageCtrl.ApplyCurrentGrid2CharacterGrid(_selectPlayer);
        }
    }
}