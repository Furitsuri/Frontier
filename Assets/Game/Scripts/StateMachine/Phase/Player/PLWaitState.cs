using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Frontier.Character;

namespace Frontier
{
    public class PLWaitState : PhaseStateBase
    {
        public override void Init()
        {
            base.Init();

            // �I�𒆂̃v���C���[���擾
            var selectPlayer = (Player)_btlMgr.GetSelectCharacter();
            if (selectPlayer == null)
            {
                Debug.Assert(false);

                return;
            }

            // �S�ẴR�}���h���I����
            var endCommand = selectPlayer.tmpParam.isEndCommand;
            endCommand[(int)Command.COMMAND_TAG.MOVE] = true;
            endCommand[(int)Command.COMMAND_TAG.ATTACK] = true;
            endCommand[(int)Command.COMMAND_TAG.WAIT] = true;

            // �X�V�����ɏI��
            Back();
        }

        public override void Exit()
        {
            base.Exit();
        }
    }
}