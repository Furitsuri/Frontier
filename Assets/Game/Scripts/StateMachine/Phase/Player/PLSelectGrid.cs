using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PLSelectGrid : PhaseStateBase
{
    override public void Init()
    {
        base.Init();

        // �O���b�h�I����L����
        BattleUISystem.Instance.ToggleSelectGrid( true );
    }

    override public bool Update()
    {
        // �O���b�h�I�����J�ڂ��߂邱�Ƃ͂Ȃ����ߊ��̍X�V�͍s��Ȃ�
        // if( base.Update() ) { return true; }

        // �S�ẴL�����N�^�[���ҋ@�ς݂ɂȂ��Ă���ΏI��
        if( BattleManager.Instance.IsEndAllCharacterWaitCommand() )
        {
            Back();

            return true;
        }

        // �^�[���I���m�F�֑J��
        if( Input.GetKeyUp( KeyCode.Escape ) )
        {
            TransitIndex = 1;
            return true;
        }

        // �O���b�h�̑���
        StageGrid.Instance.OperateCurrentGrid();
        StageGrid.GridInfo info;
        StageGrid.Instance.FetchCurrentGridInfo(out info);

        // ���݂̑I���O���b�h��ɖ��s���̃v���C���[�����݂���ꍇ�͍s���I����
        int selectCharaIndex = info.charaIndex;

        var player = BattleManager.Instance.GetCharacterFromHashtable( Character.CHARACTER_TAG.CHARACTER_PLAYER, selectCharaIndex );
        if ( player != null && !player.tmpParam.isEndCommand[(int)Character.BaseCommand.COMMAND_WAIT])
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
        // BattleUISystem.Instance.ToggleSelectGrid( false );

        base.Exit();
    }
}