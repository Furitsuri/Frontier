using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static BattleManager;

public class PLMoveState : PhaseStateBase
{
    private enum PLMovePhase{
        PL_MOVE_SELECT_GRID = 0,
        PL_MOVE_EXECUTE,
        PL_MOVE_END,
    }

    private PLMovePhase m_Phase = PLMovePhase.PL_MOVE_SELECT_GRID;
    private Player selectPlayer;
    private int departGridIndex = -1;
    private int movingIndex = 0;
    private List<int> movePathList;
    List<Vector3> moveGridPos;
    Transform PLTransform;

    override public void Init()
    {
        base.Init();

        movingIndex = 0;
        m_Phase = PLMovePhase.PL_MOVE_SELECT_GRID;
        departGridIndex = StageGrid.instance.currentGrid.GetIndex();
        movePathList = new List<int>(64);

        // 現在選択中のキャラクター情報を取得して移動範囲を表示
        selectPlayer = BattleManager.instance.SearchPlayerFromCharaIndex(BattleManager.instance.SelectCharacterIndex);
        if( selectPlayer == null )
        {
            // ASSERT表示
        }
        StageGrid.instance.DrawMoveableGrids(departGridIndex, selectPlayer.param.moveRange, TurnType.PLAYER_TURN);
    }

    public override void Update()
    {
        base.Update();

        switch( m_Phase )
        {
            case PLMovePhase.PL_MOVE_SELECT_GRID:
                // グリッドの操作
                StageGrid.instance.OperateCurrentGrid();

                // 選択したグリッドが移動可能であれば選択グリッドへ遷移
                if (Input.GetKeyUp(KeyCode.Space))
                {
                    var info = StageGrid.instance.GetCurrentGridInfo();
                    if (info.isMoveable)
                    {
                        // 移動実行処理へ遷移
                        int destIndex = StageGrid.instance.currentGrid.GetIndex();
                        m_Phase = PLMovePhase.PL_MOVE_EXECUTE;

                        // 移動グリッドを求める
                        movePathList = StageGrid.instance.ExtractDepart2DestGoalGridIndexs( departGridIndex, destIndex );

                        // PlayerをmovePathListの順に移動させる
                        moveGridPos = new List<Vector3>(movePathList.Count);
                        for (int i = 0; i < movePathList.Count; ++i)
                        {
                            // パスのインデックスからグリッド座標を得る
                            moveGridPos.Add(StageGrid.instance.GetGridInfo(movePathList[i]).charaStandPos);
                        }
                        // 処理軽減のためtranformをキャッシュ
                        PLTransform = selectPlayer.transform;

                        movingIndex = 0;
                        // 移動アニメーション開始
                        selectPlayer.setAnimator(Character.ANIME_TAG.ANIME_TAG_MOVE, true);
                        // グリッド情報更新
                        selectPlayer.tmpParam.gridIndex = destIndex;

                        m_Phase = PLMovePhase.PL_MOVE_EXECUTE;
                        

                        return;
                    }
                }
                break;
            case PLMovePhase.PL_MOVE_EXECUTE:
                Vector3 dir = (moveGridPos[movingIndex] - PLTransform.position).normalized;
                PLTransform.position += dir * Constants.CHARACTER_MOVE_SPEED * Time.deltaTime;
                PLTransform.rotation = Quaternion.LookRotation(dir);
                Vector3 afterDir = (moveGridPos[movingIndex] - PLTransform.position).normalized;
                if ( Vector3.Dot(dir, afterDir) < 0 )
                {
                    PLTransform.position = moveGridPos[movingIndex];
                    movingIndex++;

                    if( moveGridPos.Count <= movingIndex) {
                        selectPlayer.setAnimator(Character.ANIME_TAG.ANIME_TAG_MOVE, false);
                        m_Phase = PLMovePhase.PL_MOVE_END;
                    }
                }
                
                break;
            case PLMovePhase.PL_MOVE_END:
                TransitIndex = 0;
                StageGrid.instance.clearGridsCondition();
                selectPlayer.tmpParam.isEndCommand[(int)Character.BaseCommand.COMMAND_MOVE] = true;
                
                break;
        }
    }

    public override void Exit()
    {
        base.Exit();
    }

    
}
