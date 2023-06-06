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

    override public void Update()
    {
        base.Update();

        // �O���b�h�̑���
        StageGrid.instance.OperateCurrentGrid();

        // ���݂̑I���O���b�h��ɖ��s���̃v���C���[�����݂���ꍇ�͍s���I����
        int selectCharaIndex = StageGrid.instance.getCurrentGridInfo().charaIndex;

        var player = BattleManager.instance.GetPlayerFromIndex(selectCharaIndex);
        if ( player != null && !player.tmpParam.isEndCommand[(int)Character.BaseCommand.COMMAND_WAIT])
        {
            if (Input.GetKeyUp(KeyCode.Space))
            {
                TransitIndex = 0;   // �J��
                return;
            }
        }
    }

    override public void Exit()
    {
        // �O���b�h�I���𖳌���
        // ���������Ȃ��ق��������ڂ��悩�������߁A�R�����g�A�E�g
        // BattleUISystem.Instance.ToggleSelectGrid( false );

        base.Exit();
    }
}