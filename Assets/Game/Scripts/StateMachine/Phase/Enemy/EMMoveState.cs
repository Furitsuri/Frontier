using System.Collections;
using System.Collections.Generic;
using System.Xml.Xsl;
using UnityEngine;
using UnityEngine.TextCore.Text;

namespace Frontier
{
    public class EMMoveState : PhaseStateBase
    {
        private enum EMMovePhase
        {
            EM_MOVE_WAIT = 0,
            EM_MOVE_EXECUTE,
            EM_MOVE_END,
        }

        private Enemy _enemy;
        private EMMovePhase _Phase = EMMovePhase.EM_MOVE_WAIT;
        private int _departGridIndex = -1;
        private int _movingIndex = 0;
        private float _moveWaitTimer = 0f;
        private List<(int routeIndexs, int routeCost)> _movePathList;
        private List<Vector3> _moveGridPos;
        private Transform _EMTransform;

        override public void Init()
        {
            var stageGrid = Stage.StageController.Instance;

            base.Init();

            // ���ݑI�𒆂̃L�����N�^�[�����擾���Ĉړ��͈͂�\��
            _enemy = _btlMgr.GetSelectCharacter() as Enemy;
            Debug.Assert(_enemy != null);
            _departGridIndex = stageGrid.GetCurrentGridIndex();
            var param = _enemy.param;
            stageGrid.DrawMoveableGrids(_departGridIndex, param.moveRange, param.attackRange);

            _movePathList = _enemy.EmAI.GetProposedMoveRoute();
            // Enemy��_movePathList�̏��Ɉړ�������
            _moveGridPos = new List<Vector3>(_movePathList.Count);
            for (int i = 0; i < _movePathList.Count; ++i)
            {
                // �p�X�̃C���f�b�N�X����O���b�h���W�𓾂�
                _moveGridPos.Add(stageGrid.GetGridInfo(_movePathList[i].routeIndexs).charaStandPos);
            }
            _movingIndex = 0;
            _moveWaitTimer = 0f;

            // �����y���̂���tranform���L���b�V��
            _EMTransform = _enemy.transform;
            // �ړ��A�j���[�V�����J�n
            _enemy.setAnimator(Character.ANIME_TAG.MOVE, true);
            // �O���b�h���X�V
            _enemy.tmpParam.gridIndex = _enemy.EmAI.GetDestinationGridIndex();
            // �I���O���b�h��\��
            Stage.StageController.Instance.SetGridCursorActive(true);

            _Phase = EMMovePhase.EM_MOVE_WAIT;
        }

        public override bool Update()
        {
            var stageGrid = Stage.StageController.Instance;

            switch (_Phase)
            {
                case EMMovePhase.EM_MOVE_WAIT:
                    _moveWaitTimer += Time.deltaTime;
                    if (Constants.ENEMY_SHOW_MOVE_RANGE_TIME <= _moveWaitTimer)
                    {
                        // �I���O���b�h���ꎞ��\��
                        Stage.StageController.Instance.SetGridCursorActive(false);

                        _Phase = EMMovePhase.EM_MOVE_EXECUTE;
                    }

                    break;

                case EMMovePhase.EM_MOVE_EXECUTE:
                    Vector3 dir = (_moveGridPos[_movingIndex] - _EMTransform.position).normalized;
                    _EMTransform.position += dir * Constants.CHARACTER_MOVE_SPEED * Time.deltaTime;
                    _EMTransform.rotation = Quaternion.LookRotation(dir);
                    Vector3 afterDir = (_moveGridPos[_movingIndex] - _EMTransform.position).normalized;
                    if (Vector3.Dot(dir, afterDir) < 0)
                    {
                        _EMTransform.position = _moveGridPos[_movingIndex];
                        _movingIndex++;

                        if (_moveGridPos.Count <= _movingIndex)
                        {
                            _enemy.setAnimator(Character.ANIME_TAG.MOVE, false);
                            _Phase = EMMovePhase.EM_MOVE_END;
                        }
                    }
                    break;
                case EMMovePhase.EM_MOVE_END:
                    // �ړ������L�����N�^�[�̈ړ��R�}���h��I��s�ɂ���
                    _enemy.tmpParam.isEndCommand[(int)Character.Command.COMMAND_TAG.MOVE] = true;

                    // �R�}���h�I���ɖ߂�
                    Back();

                    return true;
            }

            return false;

        }

        public override void Exit()
        {
            // �G�̈ʒu�ɑI���O���b�h�����킹��
            Stage.StageController.Instance.ApplyCurrentGrid2CharacterGrid(_enemy);

            // �I���O���b�h��\��
            Stage.StageController.Instance.SetGridCursorActive(true);

            // �X�e�[�W�O���b�h��̃L���������X�V
            Stage.StageController.Instance.UpdateGridInfo();

            // �O���b�h��Ԃ̕`����N���A
            Stage.StageController.Instance.ClearGridMeshDraw();

            base.Exit();
        }
    }
}