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

    private PLMovePhase _Phase = PLMovePhase.PL_MOVE_SELECT_GRID;
    private int _departGridIndex = -1;
    private int _movingIndex = 0;
    private Player _selectPlayer;
    private List<int> _movePathList = new List<int>(64);
    private List<Vector3> _moveGridPos;
    private Transform _PLTransform;

    override public void Init()
    {
        var btlInstance = BattleManager.Instance;

        base.Init();

        _movingIndex = 0;
        _Phase = PLMovePhase.PL_MOVE_SELECT_GRID;
        _departGridIndex = StageGrid.Instance.currentGrid.GetIndex();

        // ���ݑI�𒆂̃L�����N�^�[�����擾���Ĉړ��͈͂�\��
        _selectPlayer = (Player)btlInstance.GetSelectCharacter();
        Debug.Assert(_selectPlayer != null);

        var param = _selectPlayer.param;

        // �L�����N�^�[�̌��݂̈ʒu����ێ�
        StageGrid.Footprint footprint = new StageGrid.Footprint();
        footprint.gridIndex = _selectPlayer.tmpParam.gridIndex;
        footprint.rotation = _selectPlayer.transform.rotation;
        StageGrid.Instance.LeaveFootprint( footprint );

        // �ړ��\�O���b�h��\��
        StageGrid.Instance.DrawMoveableGrids(_departGridIndex, param.moveRange, param.attackRangeMax);
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
                    var info = stageGrid.GetCurrentGridInfo();
                    if (info.isMoveable)
                    {
                        // �ړ����s�����֑J��
                        int destIndex = stageGrid.currentGrid.GetIndex();
                        _Phase = PLMovePhase.PL_MOVE_EXECUTE;

                        // �ړ��O���b�h�����߂�
                        _movePathList = stageGrid.ExtractDepart2DestGoalGridIndexs( _departGridIndex, destIndex );

                        // Player��_movePathList�̏��Ɉړ�������
                        _moveGridPos = new List<Vector3>(_movePathList.Count);
                        for (int i = 0; i < _movePathList.Count; ++i)
                        {
                            // �p�X�̃C���f�b�N�X����O���b�h���W�𓾂�
                            _moveGridPos.Add(stageGrid.GetGridInfo(_movePathList[i]).charaStandPos);
                        }
                        // �����y���̂���tranform���L���b�V��
                        _PLTransform = _selectPlayer.transform;

                        _movingIndex = 0;
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
        // �O���b�h��Ԃ̕`����N���A
        StageGrid.Instance.ClearGridsCondition();

        base.Exit();
    }
}
