using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static BattleManager;

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
        base.Init();

        movingIndex = 0;
        m_Phase = PLMovePhase.PL_MOVE_SELECT_GRID;
        departGridIndex = StageGrid.instance.currentGrid.GetIndex();
        movePathList = new List<int>(64);

        // ���ݑI�𒆂̃L�����N�^�[�����擾���Ĉړ��͈͂�\��
        selectPlayer = BattleManager.instance.SearchPlayerFromCharaIndex(BattleManager.instance.SelectCharacterIndex);
        if( selectPlayer == null )
        {
            // ASSERT�\��
        }
        StageGrid.instance.DrawMoveableGrids(departGridIndex, selectPlayer.param.moveRange, TurnType.PLAYER_TURN);
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
                    var info = StageGrid.instance.GetCurrentGridInfo();
                    if (info.isMoveable)
                    {
                        // �ړ����s�����֑J��
                        int destIndex = StageGrid.instance.currentGrid.GetIndex();
                        m_Phase = PLMovePhase.PL_MOVE_EXECUTE;

                        // �ړ��O���b�h�����߂�
                        movePathList = StageGrid.instance.ExtractDepart2DestGoalGridIndexs( departGridIndex, destIndex );

                        // Player��movePathList�̏��Ɉړ�������
                        moveGridPos = new List<Vector3>(movePathList.Count);
                        for (int i = 0; i < movePathList.Count; ++i)
                        {
                            // �p�X�̃C���f�b�N�X����O���b�h���W�𓾂�
                            moveGridPos.Add(StageGrid.instance.GetGridInfo(movePathList[i]).charaStandPos);
                        }
                        // �����y���̂���tranform���L���b�V��
                        PLTransform = selectPlayer.transform;

                        movingIndex = 0;
                        // �ړ��A�j���[�V�����J�n
                        selectPlayer.setAnimator(Character.ANIME_TAG.ANIME_TAG_MOVE, true);
                        // �O���b�h���X�V
                        selectPlayer.tmpParam.gridIndex = destIndex;

                        m_Phase = PLMovePhase.PL_MOVE_EXECUTE;
                        

                        return;
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
                        selectPlayer.setAnimator(Character.ANIME_TAG.ANIME_TAG_MOVE, false);
                        m_Phase = PLMovePhase.PL_MOVE_END;
                    }
                }
                
                break;
            case PLMovePhase.PL_MOVE_END:
                TransitIndex = 0;
                StageGrid.instance.clearGridsCondition();
                selectPlayer.tmpParam.isEndCommand[(int)Character.BaseCommand.COMMAND_MOVE] = true;
                
                break;
        }
    }

    public override void Exit()
    {
        base.Exit();
    }

    
}
