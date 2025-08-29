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

            // 選択中のプレイヤーを取得
            _selectPlayer = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter() as Player;
            // MEMO : 継承先のクラスによってはnullを許容するためnullチェックは行わない
        }

        /// <summary>
        /// キャンセル入力を受けた際の処理を行います
        /// </summary>
        /// <param name="isCancel">キャンセル入力</param>
        /// <returns>入力実行の有無</returns>
        override protected bool AcceptCancel( bool isCancel )
        {
            if ( !isCancel ) return false;

            Back();

            return true;
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