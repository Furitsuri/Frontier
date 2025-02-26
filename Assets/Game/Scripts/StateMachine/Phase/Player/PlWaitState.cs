using Frontier.Entities;
using UnityEngine;

namespace Frontier
{
    public class PlWaitState : PlPhaseStateBase
    {
        override public void Init()
        {
            base.Init();

            // 選択中のプレイヤーを取得
            var selectPlayer = (Player)_btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();
            if (selectPlayer == null)
            {
                Debug.Assert(false);

                return;
            }

            selectPlayer.EndAction();

            // 更新せずに終了
            Back();
        }

        override public void Exit()
        {
            base.Exit();
        }
    }
}