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

        // 現在選択中のキャラクター情報を取得して移動範囲を表示
        _selectPlayer = (Player)btlInstance.GetSelectCharacter();
        Debug.Assert(_selectPlayer != null);

        var param = _selectPlayer.param;

        // キャラクターの現在の位置情報を保持
        StageGrid.Footprint footprint = new StageGrid.Footprint();
        footprint.gridIndex = _selectPlayer.tmpParam.gridIndex;
        footprint.rotation  = _selectPlayer.transform.rotation;
        stgInstance.LeaveFootprint( footprint );

        // 移動可能情報を登録及び表示
        bool isAttackable = !_selectPlayer.tmpParam.isEndCommand[(int)Character.BaseCommand.COMMAND_ATTACK];
        stgInstance.RegistMoveableInfo(_departGridIndex, param.moveRange, param.attackRange, param.characterTag, isAttackable);
        stgInstance.DrawMoveableGrids(_departGridIndex, param.moveRange, param.attackRange);
    }

    public override bool Update()
    {
        var stageGrid = StageGrid.Instance;

        if( base.Update() )
        {
            // キャラクターのグリッドの位置に選択グリッドの位置を戻す
            stageGrid.FollowFootprint(_selectPlayer);

            return true;
        }

        switch( _Phase )
        {
            case PLMovePhase.PL_MOVE_SELECT_GRID:
                // グリッドの操作
                stageGrid.OperateCurrentGrid();

                // 選択したグリッドが移動可能であれば選択グリッドへ遷移
                if (Input.GetKeyUp(KeyCode.Space))
                {
                    StageGrid.GridInfo info;
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
                        _movePathList = stageGrid.ExtractShortestRouteIndexs( _departGridIndex, destIndex, candidateRouteIndexs);

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
                        BattleUISystem.Instance.ToggleSelectGrid(false);
                        // 移動アニメーション開始
                        _selectPlayer.setAnimator(Character.ANIME_TAG.MOVE, true);
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
                // 移動したキャラクターの移動コマンドを選択不可にする
                _selectPlayer.tmpParam.isEndCommand[(int)Character.BaseCommand.COMMAND_MOVE] = true;
                // コマンド選択に戻る
                Back();

                return true;
        }

        return false;
    }

    public override void Exit()
    {
        // 選択グリッドを表示
        BattleUISystem.Instance.ToggleSelectGrid(true);

        // ステージグリッド上のキャラ情報を更新
        StageGrid.Instance.UpdateGridInfo();

        // グリッド状態の描画をクリア
        StageGrid.Instance.ClearGridMeshDraw();

        base.Exit();
    }
}
