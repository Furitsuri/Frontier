using Frontier.Stage;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier
{
    public class PLSelectGridState : PhaseStateBase
    {
        override public void Init(BattleManager btlMgr, StageController stgCtrl)
        {
            base.Init(btlMgr, stgCtrl);

            // �O���b�h�I����L����
            _stageCtrl.SetGridCursorActive(true);

            // �L�[�K�C�h��o�^
            TransitKeyGuides(
                (Constants.KeyIcon.ALL_CURSOR,  "Move"),
                (Constants.KeyIcon.ESCAPE,      "TURN END"));
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

            /*
            // �L�[�K�C�h��o�^
            TransitKeyGuides(
                (Constants.KeyIcon.ALL_CURSOR, "Move"),
                (Constants.KeyIcon.ESCAPE, "TURN END"));
            */

            // �O���b�h�̑���
            _stageCtrl.OperateGridCursor();
            Stage.GridInfo info;
            _stageCtrl.FetchCurrentGridInfo(out info);

            // ���݂̑I���O���b�h��ɖ��s���̃v���C���[�����݂���ꍇ�͍s���I����
            int selectCharaIndex = info.charaIndex;

            Character character = _btlMgr.GetSelectCharacter();
            if (character != null &&
                 character.param.characterTag == Character.CHARACTER_TAG.PLAYER &&
                 !character.tmpParam.isEndCommand[(int)Character.Command.COMMAND_TAG.WAIT])
            {
                /*
                // �L�[�K�C�h��o�^
                TransitKeyGuides(
                    (Constants.KeyIcon.ALL_CURSOR,  "Move"),
                    (Constants.KeyIcon.DECISION,    "DECISION"),
                    (Constants.KeyIcon.ESCAPE,      "TURN END"));
                */

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
            // _stageCtrl.SetGridCursorActive( false );

            base.Exit();
        }
    }
}