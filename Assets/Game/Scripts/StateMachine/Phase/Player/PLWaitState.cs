using Frontier.Stage;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier
{
    public class PLWaitState : PhaseStateBase
    {
        public override void Init(BattleManager btlMgr, StageController stgCtrl)
        {
            base.Init(btlMgr, stgCtrl);

            // 選択中のプレイヤーを取得
            var selectPlayer = (Player)_btlMgr.GetSelectCharacter();
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