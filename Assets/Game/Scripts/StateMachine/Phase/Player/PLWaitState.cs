using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Character;

public class PLWaitState : PhaseStateBase
{
    public override void Init()
    {
        base.Init();

        // �I�𒆂̃v���C���[���擾
        var selectPlayer = (Player)BattleManager.instance.GetSelectCharacter();
        if (selectPlayer == null)
        {
            Debug.Assert(false);

            return;
        }

        // �S�ẴR�}���h���I����
        var endCommand = selectPlayer.tmpParam.isEndCommand;
        endCommand[(int)BaseCommand.COMMAND_MOVE]   = true;
        endCommand[(int)BaseCommand.COMMAND_ATTACK] = true;
        endCommand[(int)BaseCommand.COMMAND_WAIT]   = true;

        // �X�V�����ɏI��
        Back();
    }

    public override void Exit()
    {
        base.Exit();
    }
}
