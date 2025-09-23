using Frontier.Combat;
using Frontier.Entities;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier
{
    public class EmMoveState : PhaseStateBase
    {
        private enum EmMovePhase
        {
            EM_MOVE_WAIT = 0,
            EM_MOVE_EXECUTE,
            EM_MOVE_END,
        }

        private EmMovePhase _Phase = EmMovePhase.EM_MOVE_WAIT;
        private int _departGridIndex = -1;
        private int _movingIndex = 0;
        private float _moveWaitTimer = 0f;
        private Enemy _enemy;
        private List<WaypointInformation> _movePathList;
        private List<Vector3> _moveGridPos;
        private Transform _ownerTransform;

        /// <summary>
        /// 初期化します
        /// MEMO : 移動する経路自体は既にEmSelectStateで決定済みの想定で設計されています。
        ///        EmMoveStateの初期化時点でどこに移動するか(引いては何を行うか)を決定していては遅いためです。
        /// </summary>
        override public void Init()
        {
            base.Init();

            // 現在選択中のキャラクター情報を取得して移動範囲を表示
            _enemy = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter() as Enemy;
            Debug.Assert(_enemy != null);
            _departGridIndex = _stageCtrl.GetCurrentGridIndex();
            var param = _enemy.Params.CharacterParam;
            _stageCtrl.DrawMoveableGrids(_departGridIndex, param.moveRange, param.attackRange);

            _movePathList = _enemy.GetAi().MovePathHandler.ProposedMovePath; // _enemy.GetAi().GetProposedMovePath();

            // 移動目標地点が、現在地点であった場合は即時終了
            if (_movePathList.Count <= 0)
            {
                _Phase = EmMovePhase.EM_MOVE_END;
            }
            // 移動前処理
            else
            {
                // Enemyを_movePathListの順に移動させる
                _moveGridPos = new List<Vector3>(_movePathList.Count);
                for (int i = 0; i < _movePathList.Count; ++i)
                {
                    // パスのインデックスからグリッド座標を得る
                    _moveGridPos.Add(_stageCtrl.GetTileInfo(_movePathList[i].TileIndex).charaStandPos);
                }
                _movingIndex    = 0;
                _moveWaitTimer  = 0f;

                _ownerTransform = _enemy.transform;                                                        // 処理軽減のためtranformをキャッシュ
                _enemy.AnimCtrl.SetAnimator(AnimDatas.AnimeConditionsTag.MOVE, true);                   // 移動アニメーション開始
                _enemy.Params.TmpParam.SetCurrentGridIndex(_enemy.GetAi().GetDestinationGridIndex());   // グリッド情報更新
                _stageCtrl.SetGridCursorControllerActive(true);                                         // 選択グリッドを表示

                _Phase = EmMovePhase.EM_MOVE_WAIT;
            }
        }

        override public bool Update()
        {
            switch (_Phase)
            {
                case EmMovePhase.EM_MOVE_WAIT:
                    _moveWaitTimer += DeltaTimeProvider.DeltaTime;
                    if (Constants.ENEMY_SHOW_MOVE_RANGE_TIME <= _moveWaitTimer)
                    {
                        // 選択グリッドを一時非表示
                        _stageCtrl.SetGridCursorControllerActive(false);

                        _Phase = EmMovePhase.EM_MOVE_EXECUTE;
                    }

                    break;

                case EmMovePhase.EM_MOVE_EXECUTE:
                    Vector3 dir = (_moveGridPos[_movingIndex] - _ownerTransform.position).normalized;
                    _ownerTransform.position += dir * Constants.CHARACTER_MOVE_SPEED * DeltaTimeProvider.DeltaTime;
                    _ownerTransform.rotation = Quaternion.LookRotation(dir);
                    Vector3 afterDir = (_moveGridPos[_movingIndex] - _ownerTransform.position).normalized;
                    if (Vector3.Dot(dir, afterDir) < 0)
                    {
                        _ownerTransform.position = _moveGridPos[_movingIndex];
                        _movingIndex++;

                        if (_moveGridPos.Count <= _movingIndex)
                        {
                            _enemy.AnimCtrl.SetAnimator(AnimDatas.AnimeConditionsTag.MOVE, false);
                            _Phase = EmMovePhase.EM_MOVE_END;
                        }
                    }
                    break;
                case EmMovePhase.EM_MOVE_END:
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