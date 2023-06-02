using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static BattleManager;

public class PLMoveState : PhaseStateBase
{
    private enum PLMovePhase{
        PL_MOVE_SELECT_GRID = 0,
        PL_MOVE_EXECUTE,
        PL_MOVE_END,
    }

    private PLMovePhase m_Phase = PLMovePhase.PL_MOVE_SELECT_GRID;
    private int departGridIndex = -1;
    private List<int> movePathList;

    override public void Init()
    {
        base.Init();

        m_Phase = PLMovePhase.PL_MOVE_SELECT_GRID;
        departGridIndex = StageGrid.instance.CurrentGridIndex;
        movePathList = new List<int>(64);

        // ���ݑI�𒆂̃L�����N�^�[�����擾���Ĉړ��͈͂�\��
        Character.Parameter param = new Character.Parameter();
        BattleManager.instance.GetPlayerFromIndex(ref param, BattleManager.instance.SelectCharacterIndex);
        StageGrid.instance.DrawGridsCondition(departGridIndex, param.moveRange, TurnType.PLAYER_TURN);
    }

    public override void Update()
    {
        base.Update();

        switch( m_Phase )
        {
            case PLMovePhase.PL_MOVE_SELECT_GRID:
                // �O���b�h�̑���
                StageGrid.instance.OperateCurrentGrid();

                // �I�������O���b�h���ړ��\�ł���ΑI���O���b�h�֑J��
                if (Input.GetKeyUp(KeyCode.Space))
                {
                    var info = StageGrid.instance.getCurrentGridInfo();
                    if (info.isMoveable)
                    {
                        // �ړ����s�����֑J��
                        int destIndex = StageGrid.instance.CurrentGridIndex;
                        m_Phase = PLMovePhase.PL_MOVE_EXECUTE;

                        // �ړ��O���b�h�����߂�
                        movePathList = StageGrid.instance.extractDepart2DestGoalGridIndexs( departGridIndex, destIndex );

                        return;
                    }
                }
                break;
                case PLMovePhase.PL_MOVE_EXECUTE:
                break;
                case PLMovePhase.PL_MOVE_END:
                break;
        }
    }

    public override void Exit()
    {


        base.Exit();
    }

    
}
