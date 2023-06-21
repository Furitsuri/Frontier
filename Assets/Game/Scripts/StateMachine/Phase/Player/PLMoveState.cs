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

        // 現在選択中のキャラクター情報を取得して移動範囲を表示
        _selectPlayer = (Player)btlInstance.GetSelectCharacter();
        Debug.Assert(_selectPlayer != null);

        var param = _selectPlayer.param;

        // キャラクターの現在の位置情報を保持
        StageGrid.Footprint footprint = new StageGrid.Footprint();
        footprint.gridIndex = _selectPlayer.tmpParam.gridIndex;
        footprint.rotation = _selectPlayer.transform.rotation;
        StageGrid.Instance.LeaveFootprint( footprint );

        // 移動可能グリッドを表示
        StageGrid.Instance.DrawMoveableGrids(_departGridIndex, param.moveRange, param.attackRangeMax);
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
                    var info = stageGrid.GetCurrentGridInfo();
                    if (info.isMoveable)
                    {
                        // 移動実行処理へ遷移
                        int destIndex = stageGrid.currentGrid.GetIndex();
                        _Phase = PLMovePhase.PL_MOVE_EXECUTE;

                        // 移動グリッドを求める
                        _movePathList = stageGrid.ExtractDepart2DestGoalGridIndexs( _departGridIndex, destIndex );

                        // Playerを_movePathListの順に移動させる
                        _moveGridPos = new List<Vector3>(_movePathList.Count);
                        for (int i = 0; i < _movePathList.Count; ++i)
                        {
                            // パスのインデックスからグリッド座標を得る
                            _moveGridPos.Add(stageGrid.GetGridInfo(_movePathList[i]).charaStandPos);
                        }
                        // 処理軽減のためtranformをキャッシュ
                        _PLTransform = _selectPlayer.transform;

                        _movingIndex = 0;
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
        // グリッド状態の描画をクリア
        StageGrid.Instance.ClearGridsCondition();

        base.Exit();
    }
}
