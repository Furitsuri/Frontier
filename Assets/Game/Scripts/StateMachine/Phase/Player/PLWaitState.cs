using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Character;

public class PLWaitState : PhaseStateBase
{
    public override void Init()
    {
        base.Init();

        // 選択中のプレイヤーを取得
        var selectPlayer = (Player)BattleManager.instance.GetSelectCharacter();
        if (selectPlayer == null)
        {
            Debug.Assert(false);

            return;
        }

        // 全てのコマンドを終了に
        var endCommand = selectPlayer.tmpParam.isEndCommand;
        endCommand[(int)BaseCommand.COMMAND_MOVE]   = true;
        endCommand[(int)BaseCommand.COMMAND_ATTACK] = true;
        endCommand[(int)BaseCommand.COMMAND_WAIT]   = true;

        // 更新せずに終了
        Back();
    }

    public override void Exit()
    {
        base.Exit();
    }
}
