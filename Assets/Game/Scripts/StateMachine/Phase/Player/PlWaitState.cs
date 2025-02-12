using Frontier.Entities;
using UnityEngine;

namespace Frontier
{
    public class PlWaitState : PhaseStateBase
    {
        public override void Init()
        {
            base.Init();

            // 選択中のプレイヤーを取得
            var selectPlayer = (Player)_btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();
            if (selectPlayer == null)
            {
                Debug.Assert(false);

                return;
            }

            // 全てのコマンドを終了に
            var endCommand = selectPlayer.tmpParam.isEndCommand;
            endCommand[(int)Character.Command.COMMAND_TAG.MOVE] = true;
            endCommand[(int)Character.Command.COMMAND_TAG.ATTACK] = true;
            endCommand[(int)Character.Command.COMMAND_TAG.WAIT] = true;

            // 更新せずに終了
            Back();
        }

        public override void Exit()
        {
            base.Exit();
        }
    }
}