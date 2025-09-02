using Frontier.Combat;
using Frontier.Entities;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier
{
    public class EmMoveState : PhaseStateBase
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
            base.Init();

            // 現在選択中のキャラクター情報を取得して移動範囲を表示
            _enemy = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter() as Enemy;
            Debug.Assert(_enemy != null);
            _departGridIndex = _stageCtrl.GetCurrentGridIndex();
            var param = _enemy.Params.CharacterParam;
            _stageCtrl.DrawMoveableGrids(_departGridIndex, param.moveRange, param.attackRange);

            _movePathList = _enemy.GetAi().GetProposedMoveRoute();

            // 移動目標地点が、現在地点であった場合は即時終了
            if (_movePathList.Count <= 0)
            {
                _Phase = EMMovePhase.EM_MOVE_END;
            }
            // 移動前処理
            else
            {
                // Enemyを_movePathListの順に移動させる
                _moveGridPos = new List<Vector3>(_movePathList.Count);
                for (int i = 0; i < _movePathList.Count; ++i)
                {
                    // パスのインデックスからグリッド座標を得る
                    _moveGridPos.Add(_stageCtrl.GetGridInfo(_movePathList[i].routeIndexs).charaStandPos);
                }
                _movingIndex = 0;
                _moveWaitTimer = 0f;

                // 処理軽減のためtranformをキャッシュ
                _EMTransform = _enemy.transform;
                // 移動アニメーション開始
                _enemy.AnimCtrl.SetAnimator(AnimDatas.AnimeConditionsTag.MOVE, true);
                // グリッド情報更新
                _enemy.Params.TmpParam.SetCurrentGridIndex(_enemy.GetAi().GetDestinationGridIndex());
                // 選択グリッドを表示
                _stageCtrl.SetGridCursorControllerActive(true);

                _Phase = EMMovePhase.EM_MOVE_WAIT;
            }
        }

        override public bool Update()
        {
            switch (_Phase)
            {
                case EMMovePhase.EM_MOVE_WAIT:
                    _moveWaitTimer += DeltaTimeProvider.DeltaTime;
                    if (Constants.ENEMY_SHOW_MOVE_RANGE_TIME <= _moveWaitTimer)
                    {
                        // 選択グリッドを一時非表示
                        _stageCtrl.SetGridCursorControllerActive(false);

                        _Phase = EMMovePhase.EM_MOVE_EXECUTE;
                    }

                    break;

                case EMMovePhase.EM_MOVE_EXECUTE:
                    Vector3 dir = (_moveGridPos[_movingIndex] - _EMTransform.position).normalized;
                    _EMTransform.position += dir * Constants.CHARACTER_MOVE_SPEED * DeltaTimeProvider.DeltaTime;
                    _EMTransform.rotation = Quaternion.LookRotation(dir);
                    Vector3 afterDir = (_moveGridPos[_movingIndex] - _EMTransform.position).normalized;
                    if (Vector3.Dot(dir, afterDir) < 0)
                    {
                        _EMTransform.position = _moveGridPos[_movingIndex];
                        _movingIndex++;

                        if (_moveGridPos.Count <= _movingIndex)
                        {
                            _enemy.AnimCtrl.SetAnimator(AnimDatas.AnimeConditionsTag.MOVE, false);
                            _Phase = EMMovePhase.EM_MOVE_END;
                        }
                    }
                    break;
                case EMMovePhase.EM_MOVE_END:
                    // 移動したキャラクターの移動コマンドを選択不可にする
                    _enemy.Params.TmpParam.SetEndCommandStatus(Command.COMMAND_TAG.MOVE, true);

                    // コマンド選択に戻る
                    Back();

                    return true;
            }

            return false;

        }

        override public void ExitState()
        {
            // 敵の位置に選択グリッドを合わせる
            _stageCtrl.ApplyCurrentGrid2CharacterGrid(_enemy);

            // 選択グリッドを表示
            _stageCtrl.SetGridCursorControllerActive(true);

            // ステージグリッド上のキャラ情報を更新
            _stageCtrl.UpdateGridInfo();

            // グリッド状態の描画をクリア
            _stageCtrl.ClearGridMeshDraw();

            base.ExitState();
        }
    }
}