using Frontier.Entities;
using UnityEngine;

namespace Frontier
{
    public class PlWaitState : PlPhaseStateBase
    {
        override public void Init()
        {
            base.Init();

            // 選択しているプレイヤーの行動をすべて終了
            _selectPlayer.Params.TmpParam.EndAction();

            // 更新せずに終了
            Back();
        }

        /// <summary>
        /// 操作対象のプレイヤーを設定します
        /// </summary>
        override protected void AdaptSelectPlayer()
        {
            // グリッドカーソルで選択中のプレイヤーを取得
            _selectPlayer = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter() as Player;
            NullCheck.AssertNotNull( _selectPlayer, nameof( _selectPlayer ) );
        }
    }
}