using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier
{
    public class PLSelectGrid : PhaseStateBase
    {
        override public void Init()
        {
            base.Init();

            // �O���b�h�I����L����
            Stage.StageController.Instance.SetGridCursorActive(true);
        }

        override public bool Update()
        {
            // �O���b�h�I�����J�ڂ��߂邱�Ƃ͂Ȃ����ߊ��̍X�V�͍s��Ȃ�
            // if( base.Update() ) { return true; }

            // �S�ẴL�����N�^�[���ҋ@�ς݂ɂȂ��Ă���ΏI��
            if (_btlMgr.IsEndAllCharacterWaitCommand())
            {
                Back();

                return true;
            }

            // �^�[���I���m�F�֑J��
            if (Input.GetKeyUp(KeyCode.Escape))
            {
                TransitIndex = 1;
                return true;
            }

            // �O���b�h�̑���
            Stage.StageController.Instance.OperateGridCursor();
            Stage.GridInfo info;
            Stage.StageController.Instance.FetchCurrentGridInfo(out info);

            // ���݂̑I���O���b�h��ɖ��s���̃v���C���[�����݂���ꍇ�͍s���I����
            int selectCharaIndex = info.charaIndex;

            Character character = _btlMgr.GetSelectCharacter();
            if (character != null &&
                 character.param.characterTag == Character.CHARACTER_TAG.PLAYER &&
                 !character.tmpParam.isEndCommand[(int)Character.Command.COMMAND_TAG.WAIT])
            {
                if (Input.GetKeyUp(KeyCode.Space))
                {
                    TransitIndex = 0;   // �J��

                    return true;
                }
            }

            return false;
        }

        override public void Exit()
        {
            // �O���b�h�I���𖳌��� �� ���������Ȃ��ق��������ڂ��悩�������߁A�R�����g�A�E�g
            // Stage.StageController.Instance.SetGridCursorActive( false );

            base.Exit();
        }
    }
}