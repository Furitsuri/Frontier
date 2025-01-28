using Frontier.Stage;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier
{
    public class PLSelectGridState : PhaseStateBase
    {
        /// <summary>
        /// �J�ڐ�������^�O
        /// </summary>
        enum TransitTag
        {
            CharacterCommand = 0,
            TurnEnd,
        }

        override public void Init()
        {
            base.Init();

            // �O���b�h�I����L����
            _stageCtrl.SetGridCursorActive(true);

            // �L�[�K�C�h��o�^
            SetInputGuides(
                (Constants.KeyIcon.ALL_CURSOR,  "Move",     null),
                (Constants.KeyIcon.ESCAPE,      "TURN END", TransitConfirmTurnEndCallBack));
        }

        override public bool Update()
        {
            // �O���b�h�I�����J�ڂ��߂邱�Ƃ͂Ȃ����ߊ��̍X�V�͍s��Ȃ�
            // if( base.Update() ) { return true; }

            // �S�ẴL�����N�^�[���ҋ@�ς݂ɂȂ��Ă���ΏI��
            if( _btlMgr.BtlCharaCdr.IsEndAllArmyrWaitCommand(Character.CHARACTER_TAG.PLAYER))
            {
                Back();

                return true;
            }

            // TODO : �L�[�}�l�[�W�����ɑ��쏈�������S�Ɉڂ������̂��߁A��U�R�����g�A�E�g
            /*
            // �^�[���I���m�F�֑J��
            if (Input.GetKeyUp(KeyCode.Escape))
            {
                TransitIndex = (int)TransitTag.TurnEnd;
                return true;
            }
            */

            // �O���b�h�̑���
            _stageCtrl.OperateGridCursor();
            Stage.GridInfo info;
            _stageCtrl.FetchCurrentGridInfo(out info);

            // ���݂̑I���O���b�h��ɖ��s���̃v���C���[�����݂���ꍇ�͍s���I����
            int selectCharaIndex = info.charaIndex;

            // TODO : �L�[�}�l�[�W�����ɑ��쏈�������S�Ɉڂ������̂��߁A��U�R�����g�A�E�g
            /*
            Character character = _btlMgr.GetSelectCharacter();
            if (character != null &&
                 character.param.characterTag == Character.CHARACTER_TAG.PLAYER &&
                 !character.tmpParam.isEndCommand[(int)Character.Command.COMMAND_TAG.WAIT])
            {
                if (Input.GetKeyUp(KeyCode.Space))
                {
                    TransitIndex = (int)TransitTag.CharacterCommand;

                    return true;
                }
            }
            */

            return ( 0 <= TransitIndex );
        }

        override public void Exit()
        {
            // �O���b�h�I���𖳌��� �� TODO : ���������Ȃ��ق����Q�[�����s���ɂ����錩���ڂ��悩�������߁A��U�R�����g�A�E�g�ŕۗ�
            // _stageCtrl.SetGridCursorActive( false );

            base.Exit();
        }

        public void OperateGridCursorCallBack()
        {

            _stageCtrl.OperateGridCursor(Constants.Direction.LEFT);
        }

        /// <summary>
        /// �L�����N�^�[�R�}���h�J�ڂֈڂ�ۂ̃R�[���o�b�N�֐�
        /// </summary>
        /// <returns></returns>
        public bool TransitCharacterCommandCallBack()
        {
            if ( 0 <= TransitIndex )
            {
                return false;
            }

            Character character = _btlMgr.BtlCharaCdr.GetSelectCharacter();
            if (character != null &&
                 character.param.characterTag == Character.CHARACTER_TAG.PLAYER &&
                 !character.tmpParam.isEndCommand[(int)Character.Command.COMMAND_TAG.WAIT])
            {
                TransitIndex = (int)TransitTag.CharacterCommand;

                return true;
            }

            return false;
        }

        /// <summary>
        /// �^�[���I���J�ڂֈڂ�ۂ̃R�[���o�b�N�֐�
        /// </summary>
        /// <returns></returns>
        public bool TransitConfirmTurnEndCallBack()
        {
            if( 0 <= TransitIndex )
            {
                return false;
            }

            TransitIndex = (int)TransitTag.TurnEnd;

            return true;
        }
    }
}