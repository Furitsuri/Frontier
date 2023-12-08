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

            // 現在選択中のキャラクター情報を取得して移動範囲を表示
            _selectPlayer = (Player)_btlMgr.GetSelectCharacter();
            Debug.Assert(_selectPlayer != null);

            var param = _selectPlayer.param;

            // キャラクターの現在の位置情報を保持
            Stage.StageController.Footprint footprint = new Stage.StageController.Footprint();
            footprint.gridIndex = _selectPlayer.tmpParam.gridIndex;
            footprint.rotation = _selectPlayer.transform.rotation;
            _stageCtrl.LeaveFootprint(footprint);
            _stageCtrl.BindGridCursorState( GridCursor.State.MOVE, _selectPlayer);

            // 移動可能情報を登録及び表示
            bool isAttackable = !_selectPlayer.tmpParam.isEndCommand[(int)Character.Command.COMMAND_TAG.ATTACK];
            _stageCtrl.RegistMoveableInfo(_departGridIndex, param.moveRange, param.attackRange, param.characterIndex, param.characterTag, isAttackable);
            _stageCtrl.DrawMoveableGrids(_departGridIndex, param.moveRange, param.attackRange);
        }

        public override bool Update()
        {
            var stageGrid = _stageCtrl;

            if (base.Update())
            {
                // キャラクターのグリッドの位置に選択グリッドの位置を戻す
                stageGrid.FollowFootprint(_selectPlayer);

                return true;
            }

            switch (_Phase)
            {
                case PLMovePhase.PL_MOVE_SELECT_GRID:
                    // グリッドの操作
                    stageGrid.OperateGridCursor();

                    // 選択したグリッドが移動可能であれば選択グリッドへ遷移
                    if (Input.GetKeyUp(KeyCode.Space))
                    {
                        Stage.GridInfo info;
                        stageGrid.FetchCurrentGridInfo(out info);

                        if (0 <= info.estimatedMoveRange)
                        {
                            // 移動実行処理へ遷移
                            int destIndex = stageGrid.GetCurrentGridIndex();
                            _Phase = PLMovePhase.PL_MOVE_EXECUTE;

                            // 移動候補を登録し、最短経路を求める
                            List<int> candidateRouteIndexs = new List<int>(64);
                            candidateRouteIndexs.Add(_departGridIndex);
                            for (int i = 0; i < stageGrid.GridTotalNum; ++i)
                            {
                                if (0 <= stageGrid.GetGridInfo(i).estimatedMoveRange)
                                {
                                    candidateRouteIndexs.Add(i);  // 移動可能グリッドのみ抜き出す
                                }
                            }
                            _movePathList = stageGrid.ExtractShortestRouteIndexs(_departGridIndex, destIndex, candidateRouteIndexs);

                            // Playerを_movePathListの順に移動させる
                            _moveGridPos = new List<Vector3>(_movePathList.Count);
                            for (int i = 0; i < _movePathList.Count; ++i)
                            {
                                // パスのインデックスからグリッド座標を得る
                                _moveGridPos.Add(stageGrid.GetGridInfo(_movePathList[i].routeIndexs).charaStandPos);
                            }
                            // 処理軽減のためtranformをキャッシュ
                            _PLTransform = _selectPlayer.transform;

                            _movingIndex = 0;
                            // 選択グリッドを一時非表示
                            _stageCtrl.SetGridCursorActive(false);
                            // 移動アニメーション開始
                            _selectPlayer.SetAnimator(AnimDatas.ANIME_CONDITIONS_TAG.MOVE, true);
                            // グリッド情報更新
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
                    // 対象プレイヤーの操作
                    if (_selectPlayer.IsAcceptableMovementOperation( stageGrid.GetGridSize() ))
                    {
                        stageGrid.OperateGridCursor();
                    }

                    // 移動目的座標の更新
                    Stage.GridInfo infor;
                    var curGridIndex = stageGrid.GetCurrentGridIndex();
                    var plGridIndex = _selectPlayer.tmpParam.gridIndex;
                    stageGrid.FetchCurrentGridInfo(out infor);

                    // 移動更新
                    _selectPlayer.UpdateMove(curGridIndex, infor);

                    if (0 <= infor.estimatedMoveRange)
                    {
                        // 隣り合うグリッドが選択された場合はアニメーションを用いた連続的な移動
                        // if (stageGrid.IsGridNextToEacheOther(curGridIndex, plGridIndex))
                        {
                            
                        }
                        // 離れたグリッドが選択された場合は瞬時に移動
                        // else
                        // {
                        //     _selectPlayer.SetPosition(curGridIndex, _selectPlayer.transform.rotation);
                        // }

                        if (Input.GetKeyUp(KeyCode.Space))
                        {
                            // 出発地点と同一グリッドであれば戻る
                            if (curGridIndex == _departGridIndex)
                                Back();
                            // キャラクターが存在していないことを確認
                            else if( 0 == ( infor.flag & ( StageController.BitFlag.PLAYER_EXIST | StageController.BitFlag.ENEMY_EXIST | StageController.BitFlag.OTHER_EXIST)) )
                                _Phase = PLMovePhase.PL_MOVE_END;
                            break;
                        }
                    }
                    break;
                case PLMovePhase.PL_MOVE_END:
                    // 移動したキャラクターの移動コマンドを選択不可にする
                    _selectPlayer.tmpParam.isEndCommand[(int)Character.Command.COMMAND_TAG.MOVE] = true;
                    // コマンド選択に戻る
                    Back();

                    return true;
            }

            return false;
        }

        public override void Exit()
        {
            // 操作対象データをリセット
            _stageCtrl.ClearGridCursroBind();

            // 選択グリッドを表示
            _stageCtrl.SetGridCursorActive(true);

            // ステージグリッド上のキャラ情報を更新
            _stageCtrl.UpdateGridInfo();

            // グリッド状態の描画をクリア
            _stageCtrl.ClearGridMeshDraw();

            base.Exit();
        }
    }
}