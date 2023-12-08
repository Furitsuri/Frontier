using Frontier.Stage;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Frontier
{
    public class PLMoveState : PhaseStateBase
    {
        private enum PLMovePhase
        {
            PL_MOVE_SELECT_GRID = 0,
            PL_MOVE_EXECUTE,
            PL_MOVE,
            PL_MOVE_END,
        }

        private PLMovePhase _Phase = PLMovePhase.PL_MOVE_SELECT_GRID;
        private int _departGridIndex = -1;
        private int _movingIndex = 0;
        private Player _selectPlayer = null;
        private List<(int routeIndexs, int routeCost)> _movePathList = new List<(int, int)>(Constants.DIJKSTRA_ROUTE_INDEXS_MAX_NUM);
        private List<Vector3> _moveGridPos;
        private Transform _PLTransform;

        override public void Init(BattleManager btlMgr, StageController stgCtrl)
        {
            base.Init(btlMgr, stgCtrl);

            _movingIndex = 0;
            _Phase = PLMovePhase.PL_MOVE;
            _departGridIndex = _stageCtrl.GetCurrentGridIndex();

            // ���ݑI�𒆂̃L�����N�^�[�����擾���Ĉړ��͈͂�\��
            _selectPlayer = (Player)_btlMgr.GetSelectCharacter();
            Debug.Assert(_selectPlayer != null);

            var param = _selectPlayer.param;

            // �L�����N�^�[�̌��݂̈ʒu����ێ�
            Stage.StageController.Footprint footprint = new Stage.StageController.Footprint();
            footprint.gridIndex = _selectPlayer.tmpParam.gridIndex;
            footprint.rotation = _selectPlayer.transform.rotation;
            _stageCtrl.LeaveFootprint(footprint);
            _stageCtrl.BindGridCursorState( GridCursor.State.MOVE, _selectPlayer);

            // �ړ��\����o�^�y�ѕ\��
            bool isAttackable = !_selectPlayer.tmpParam.isEndCommand[(int)Character.Command.COMMAND_TAG.ATTACK];
            _stageCtrl.RegistMoveableInfo(_departGridIndex, param.moveRange, param.attackRange, param.characterIndex, param.characterTag, isAttackable);
            _stageCtrl.DrawMoveableGrids(_departGridIndex, param.moveRange, param.attackRange);
        }

        public override bool Update()
        {
            var stageGrid = _stageCtrl;

            if (base.Update())
            {
                // �L�����N�^�[�̃O���b�h�̈ʒu�ɑI���O���b�h�̈ʒu��߂�
                stageGrid.FollowFootprint(_selectPlayer);

                return true;
            }

            switch (_Phase)
            {
                case PLMovePhase.PL_MOVE_SELECT_GRID:
                    // �O���b�h�̑���
                    stageGrid.OperateGridCursor();

                    // �I�������O���b�h���ړ��\�ł���ΑI���O���b�h�֑J��
                    if (Input.GetKeyUp(KeyCode.Space))
                    {
                        Stage.GridInfo info;
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
                            _movePathList = stageGrid.ExtractShortestRouteIndexs(_departGridIndex, destIndex, candidateRouteIndexs);

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
                            _stageCtrl.SetGridCursorActive(false);
                            // �ړ��A�j���[�V�����J�n
                            _selectPlayer.SetAnimator(AnimDatas.ANIME_CONDITIONS_TAG.MOVE, true);
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
                    if (Vector3.Dot(dir, afterDir) < 0)
                    {
                        _PLTransform.position = _moveGridPos[_movingIndex];
                        _movingIndex++;

                        if (_moveGridPos.Count <= _movingIndex)
                        {
                            _selectPlayer.SetAnimator(AnimDatas.ANIME_CONDITIONS_TAG.MOVE, false);
                            _Phase = PLMovePhase.PL_MOVE_END;
                        }
                    }
                    break;
                case PLMovePhase.PL_MOVE:
                    // �Ώۃv���C���[�̑���
                    if (_selectPlayer.IsAcceptableMovementOperation( stageGrid.GetGridSize() ))
                    {
                        stageGrid.OperateGridCursor();
                    }

                    // �ړ��ړI���W�̍X�V
                    Stage.GridInfo infor;
                    var curGridIndex = stageGrid.GetCurrentGridIndex();
                    var plGridIndex = _selectPlayer.tmpParam.gridIndex;
                    stageGrid.FetchCurrentGridInfo(out infor);

                    // �ړ��X�V
                    _selectPlayer.UpdateMove(curGridIndex, infor);

                    if (0 <= infor.estimatedMoveRange)
                    {
                        // �ׂ荇���O���b�h���I�����ꂽ�ꍇ�̓A�j���[�V������p�����A���I�Ȉړ�
                        // if (stageGrid.IsGridNextToEacheOther(curGridIndex, plGridIndex))
                        {
                            
                        }
                        // ���ꂽ�O���b�h���I�����ꂽ�ꍇ�͏u���Ɉړ�
                        // else
                        // {
                        //     _selectPlayer.SetPosition(curGridIndex, _selectPlayer.transform.rotation);
                        // }

                        if (Input.GetKeyUp(KeyCode.Space))
                        {
                            // �o���n�_�Ɠ���O���b�h�ł���Ζ߂�
                            if (curGridIndex == _departGridIndex)
                                Back();
                            // �L�����N�^�[�����݂��Ă��Ȃ����Ƃ��m�F
                            else if( 0 == ( infor.flag & ( StageController.BitFlag.PLAYER_EXIST | StageController.BitFlag.ENEMY_EXIST | StageController.BitFlag.OTHER_EXIST)) )
                                _Phase = PLMovePhase.PL_MOVE_END;
                            break;
                        }
                    }
                    break;
                case PLMovePhase.PL_MOVE_END:
                    // �ړ������L�����N�^�[�̈ړ��R�}���h��I��s�ɂ���
                    _selectPlayer.tmpParam.isEndCommand[(int)Character.Command.COMMAND_TAG.MOVE] = true;
                    // �R�}���h�I���ɖ߂�
                    Back();

                    return true;
            }

            return false;
        }

        public override void Exit()
        {
            // ����Ώۃf�[�^�����Z�b�g
            _stageCtrl.ClearGridCursroBind();

            // �I���O���b�h��\��
            _stageCtrl.SetGridCursorActive(true);

            // �X�e�[�W�O���b�h��̃L���������X�V
            _stageCtrl.UpdateGridInfo();

            // �O���b�h��Ԃ̕`����N���A
            _stageCtrl.ClearGridMeshDraw();

            base.Exit();
        }
    }
}