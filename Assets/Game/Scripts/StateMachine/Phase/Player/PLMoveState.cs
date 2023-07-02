using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static StageGrid;

public class PLMoveState : PhaseStateBase
{
    private enum PLMovePhase{
        PL_MOVE_SELECT_GRID = 0,
        PL_MOVE_EXECUTE,
        PL_MOVE_END,
    }

    private PLMovePhase _Phase = PLMovePhase.PL_MOVE_SELECT_GRID;
    private int _departGridIndex = -1;
    private int _movingIndex = 0;
    private Player _selectPlayer;
    private List<(int routeIndexs, int routeCost)> _movePathList = new List<(int, int)>(Constants.DIJKSTRA_ROUTE_INDEXS_MAX_NUM);
    private List<Vector3> _moveGridPos;
    private Transform _PLTransform;

    override public void Init()
    {
        var btlInstance = BattleManager.Instance;
        var stgInstance = StageGrid.Instance;

        base.Init();

        _movingIndex = 0;
        _Phase = PLMovePhase.PL_MOVE_SELECT_GRID;
        _departGridIndex = StageGrid.Instance.GetCurrentGridIndex();

        // ���ݑI�𒆂̃L�����N�^�[�����擾���Ĉړ��͈͂�\��
        _selectPlayer = (Player)btlInstance.GetSelectCharacter();
        Debug.Assert(_selectPlayer != null);

        var param = _selectPlayer.param;

        // �L�����N�^�[�̌��݂̈ʒu����ێ�
        StageGrid.Footprint footprint = new StageGrid.Footprint();
        footprint.gridIndex = _selectPlayer.tmpParam.gridIndex;
        footprint.rotation  = _selectPlayer.transform.rotation;
        stgInstance.LeaveFootprint( footprint );

        // �ړ��\����o�^�y�ѕ\��
        bool isAttackable = !_selectPlayer.tmpParam.isEndCommand[(int)Character.BaseCommand.COMMAND_ATTACK];
        stgInstance.RegistMoveableInfo(_departGridIndex, param.moveRange, param.attackRange, param.characterTag, isAttackable);
        stgInstance.DrawMoveableGrids(_departGridIndex, param.moveRange, param.attackRange);
    }

    public override bool Update()
    {
        var stageGrid = StageGrid.Instance;

        if( base.Update() )
        {
            // �L�����N�^�[�̃O���b�h�̈ʒu�ɑI���O���b�h�̈ʒu��߂�
            stageGrid.FollowFootprint(_selectPlayer);

            return true;
        }

        switch( _Phase )
        {
            case PLMovePhase.PL_MOVE_SELECT_GRID:
                // �O���b�h�̑���
                stageGrid.OperateCurrentGrid();

                // �I�������O���b�h���ړ��\�ł���ΑI���O���b�h�֑J��
                if (Input.GetKeyUp(KeyCode.Space))
                {
                    StageGrid.GridInfo info;
                    stageGrid.FetchCurrentGridInfo(out info);

                    if (0 <= info.estimatedMoveRange)
                    {
                        // �ړ����s�����֑J��
                        int destIndex = stageGrid.GetCurrentGridIndex();
                        _Phase = PLMovePhase.PL_MOVE_EXECUTE;

                        // �ړ�����o�^���A�ŒZ�o�H�����߂�
                        List<int> candidateRouteIndexs = new List<int>(64);
                        candidateRouteIndexs.Add(_departGridIndex);
                        for (int i = 0; i < stageGrid.GridTotalNum; ++i)
                        {
                            if (0 <= stageGrid.GetGridInfo(i).estimatedMoveRange)
                            {
                                candidateRouteIndexs.Add(i);  // �ړ��\�O���b�h�̂ݔ����o��
                            }
                        }
                        _movePathList = stageGrid.ExtractShortestRouteIndexs( _departGridIndex, destIndex, candidateRouteIndexs);

                        // Player��_movePathList�̏��Ɉړ�������
                        _moveGridPos = new List<Vector3>(_movePathList.Count);
                        for (int i = 0; i < _movePathList.Count; ++i)
                        {
                            // �p�X�̃C���f�b�N�X����O���b�h���W�𓾂�
                            _moveGridPos.Add(stageGrid.GetGridInfo(_movePathList[i].routeIndexs).charaStandPos);
                        }
                        // �����y���̂���tranform���L���b�V��
                        _PLTransform = _selectPlayer.transform;

                        _movingIndex = 0;
                        // �I���O���b�h���ꎞ��\��
                        BattleUISystem.Instance.ToggleSelectGrid(false);
                        // �ړ��A�j���[�V�����J�n
                        _selectPlayer.setAnimator(Character.ANIME_TAG.MOVE, true);
                        // �O���b�h���X�V
                        _selectPlayer.tmpParam.gridIndex = destIndex;

                        _Phase = PLMovePhase.PL_MOVE_EXECUTE;
                    }
                }
                break;
            case PLMovePhase.PL_MOVE_EXECUTE:
                Vector3 dir = (_moveGridPos[_movingIndex] - _PLTransform.position).normalized;
                _PLTransform.position += dir * Constants.CHARACTER_MOVE_SPEED * Time.deltaTime;
                _PLTransform.rotation = Quaternion.LookRotation(dir);
                Vector3 afterDir = (_moveGridPos[_movingIndex] - _PLTransform.position).normalized;
                if ( Vector3.Dot(dir, afterDir) < 0 )
                {
                    _PLTransform.position = _moveGridPos[_movingIndex];
                    _movingIndex++;

                    if( _moveGridPos.Count <= _movingIndex) {
                        _selectPlayer.setAnimator(Character.ANIME_TAG.MOVE, false);
                        _Phase = PLMovePhase.PL_MOVE_END;
                    }
                }
                break;
            case PLMovePhase.PL_MOVE_END: 
                // �ړ������L�����N�^�[�̈ړ��R�}���h��I��s�ɂ���
                _selectPlayer.tmpParam.isEndCommand[(int)Character.BaseCommand.COMMAND_MOVE] = true;
                // �R�}���h�I���ɖ߂�
                Back();

                return true;
        }

        return false;
    }

    public override void Exit()
    {
        // �I���O���b�h��\��
        BattleUISystem.Instance.ToggleSelectGrid(true);

        // �X�e�[�W�O���b�h��̃L���������X�V
        StageGrid.Instance.UpdateGridInfo();

        // �O���b�h��Ԃ̕`����N���A
        StageGrid.Instance.ClearGridMeshDraw();

        base.Exit();
    }
}
