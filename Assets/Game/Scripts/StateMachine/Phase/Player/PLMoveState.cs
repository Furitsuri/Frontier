using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PLMoveState : PhaseStateBase
{
    private enum PLMovePhase{
        PL_MOVE_SELECT_GRID = 0,
        PL_MOVE_EXECUTE,
        PL_MOVE_END,
    }

    private PLMovePhase m_Phase = PLMovePhase.PL_MOVE_SELECT_GRID;
    private Player selectPlayer;
    private int departGridIndex = -1;
    private int movingIndex = 0;
    private List<int> movePathList;
    List<Vector3> moveGridPos;
    Transform PLTransform;

    override public void Init()
    {
        var btlInstance = BattleManager.Instance;

        base.Init();

        movingIndex = 0;
        m_Phase = PLMovePhase.PL_MOVE_SELECT_GRID;
        departGridIndex = StageGrid.Instance.currentGrid.GetIndex();
        movePathList = new List<int>(64);

        // ���ݑI�𒆂̃L�����N�^�[�����擾���Ĉړ��͈͂�\��
        selectPlayer = (Player)btlInstance.GetSelectCharacter();
        Debug.Assert(selectPlayer != null);

        var param = selectPlayer.param;

        // �L�����N�^�[�̌��݂̈ʒu����ێ�
        StageGrid.Footprint footprint = new StageGrid.Footprint();
        footprint.gridIndex = selectPlayer.tmpParam.gridIndex;
        footprint.rotation = selectPlayer.transform.rotation;
        StageGrid.Instance.LeaveFootprint( footprint );

        // �ړ��\�O���b�h��\��
        StageGrid.Instance.DrawMoveableGrids(departGridIndex, param.moveRange, param.attackRangeMax);
    }

    public override bool Update()
    {
        if( base.Update() )
        {
            // �L�����N�^�[�̃O���b�h�̈ʒu�ɑI���O���b�h�̈ʒu��߂�
            StageGrid.Instance.FollowFootprint(selectPlayer);

            return true;
        }

        switch( m_Phase )
        {
            case PLMovePhase.PL_MOVE_SELECT_GRID:
                // �O���b�h�̑���
                StageGrid.Instance.OperateCurrentGrid();

                // �I�������O���b�h���ړ��\�ł���ΑI���O���b�h�֑J��
                if (Input.GetKeyUp(KeyCode.Space))
                {
                    var info = StageGrid.Instance.GetCurrentGridInfo();
                    if (info.isMoveable)
                    {
                        // �ړ����s�����֑J��
                        int destIndex = StageGrid.Instance.currentGrid.GetIndex();
                        m_Phase = PLMovePhase.PL_MOVE_EXECUTE;

                        // �ړ��O���b�h�����߂�
                        movePathList = StageGrid.Instance.ExtractDepart2DestGoalGridIndexs( departGridIndex, destIndex );

                        // Player��movePathList�̏��Ɉړ�������
                        moveGridPos = new List<Vector3>(movePathList.Count);
                        for (int i = 0; i < movePathList.Count; ++i)
                        {
                            // �p�X�̃C���f�b�N�X����O���b�h���W�𓾂�
                            moveGridPos.Add(StageGrid.Instance.GetGridInfo(movePathList[i]).charaStandPos);
                        }
                        // �����y���̂���tranform���L���b�V��
                        PLTransform = selectPlayer.transform;

                        movingIndex = 0;
                        // �ړ��A�j���[�V�����J�n
                        selectPlayer.setAnimator(Character.ANIME_TAG.MOVE, true);
                        // �O���b�h���X�V
                        selectPlayer.tmpParam.gridIndex = destIndex;

                        m_Phase = PLMovePhase.PL_MOVE_EXECUTE;
                    }
                }
                break;
            case PLMovePhase.PL_MOVE_EXECUTE:
                Vector3 dir = (moveGridPos[movingIndex] - PLTransform.position).normalized;
                PLTransform.position += dir * Constants.CHARACTER_MOVE_SPEED * Time.deltaTime;
                PLTransform.rotation = Quaternion.LookRotation(dir);
                Vector3 afterDir = (moveGridPos[movingIndex] - PLTransform.position).normalized;
                if ( Vector3.Dot(dir, afterDir) < 0 )
                {
                    PLTransform.position = moveGridPos[movingIndex];
                    movingIndex++;

                    if( moveGridPos.Count <= movingIndex) {
                        selectPlayer.setAnimator(Character.ANIME_TAG.MOVE, false);
                        m_Phase = PLMovePhase.PL_MOVE_END;
                    }
                }
                break;
            case PLMovePhase.PL_MOVE_END: 
                // �ړ������L�����N�^�[�̈ړ��R�}���h��I��s�ɂ���
                selectPlayer.tmpParam.isEndCommand[(int)Character.BaseCommand.COMMAND_MOVE] = true;
                // �R�}���h�I���ɖ߂�
                Back();

                return true;
        }

        return false;
    }

    public override void Exit()
    {
        // �O���b�h��Ԃ̕`����N���A
        StageGrid.Instance.ClearGridsCondition();

        base.Exit();
    }
}
