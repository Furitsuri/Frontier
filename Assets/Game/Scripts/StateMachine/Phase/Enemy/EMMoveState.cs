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

            // 現在選択中のキャラクター情報を取得して移動範囲を表示
            _enemy = _btlMgr.GetSelectCharacter() as Enemy;
            Debug.Assert(_enemy != null);
            _departGridIndex = stageGrid.GetCurrentGridIndex();
            var param = _enemy.param;
            stageGrid.DrawMoveableGrids(_departGridIndex, param.moveRange, param.attackRange);

            _movePathList = _enemy.EmAI.GetProposedMoveRoute();
            // Enemyを_movePathListの順に移動させる
            _moveGridPos = new List<Vector3>(_movePathList.Count);
            for (int i = 0; i < _movePathList.Count; ++i)
            {
                // パスのインデックスからグリッド座標を得る
                _moveGridPos.Add(stageGrid.GetGridInfo(_movePathList[i].routeIndexs).charaStandPos);
            }
            _movingIndex = 0;
            _moveWaitTimer = 0f;

            // 処理軽減のためtranformをキャッシュ
            _EMTransform = _enemy.transform;
            // 移動アニメーション開始
            _enemy.setAnimator(Character.ANIME_TAG.MOVE, true);
            // グリッド情報更新
            _enemy.tmpParam.gridIndex = _enemy.EmAI.GetDestinationGridIndex();
            // 選択グリッドを表示
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
                        // 選択グリッドを一時非表示
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
                    // 移動したキャラクターの移動コマンドを選択不可にする
                    _enemy.tmpParam.isEndCommand[(int)Character.Command.COMMAND_TAG.MOVE] = true;

                    // コマンド選択に戻る
                    Back();

                    return true;
            }

            return false;

        }

        public override void Exit()
        {
            // 敵の位置に選択グリッドを合わせる
            Stage.StageController.Instance.ApplyCurrentGrid2CharacterGrid(_enemy);

            // 選択グリッドを表示
            Stage.StageController.Instance.SetGridCursorActive(true);

            // ステージグリッド上のキャラ情報を更新
            Stage.StageController.Instance.UpdateGridInfo();

            // グリッド状態の描画をクリア
            Stage.StageController.Instance.ClearGridMeshDraw();

            base.Exit();
        }
    }
}