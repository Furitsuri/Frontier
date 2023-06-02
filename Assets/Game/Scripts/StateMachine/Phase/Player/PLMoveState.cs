using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static BattleManager;

public class PLMoveState : PhaseStateBase
{
    private enum PLMovePhase{
        PL_MOVE_SELECT_GRID = 0,
        PL_MOVE_EXECUTE,
        PL_MOVE_END,
    }

    private PLMovePhase m_Phase = PLMovePhase.PL_MOVE_SELECT_GRID;
    private int departGridIndex = -1;
    private List<int> movePathList;

    override public void Init()
    {
        base.Init();

        m_Phase = PLMovePhase.PL_MOVE_SELECT_GRID;
        departGridIndex = StageGrid.instance.CurrentGridIndex;
        movePathList = new List<int>(64);

        // 現在選択中のキャラクター情報を取得して移動範囲を表示
        Character.Parameter param = new Character.Parameter();
        BattleManager.instance.GetPlayerFromIndex(ref param, BattleManager.instance.SelectCharacterIndex);
        StageGrid.instance.DrawGridsCondition(departGridIndex, param.moveRange, TurnType.PLAYER_TURN);
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
                    var info = StageGrid.instance.getCurrentGridInfo();
                    if (info.isMoveable)
                    {
                        // 移動実行処理へ遷移
                        int destIndex = StageGrid.instance.CurrentGridIndex;
                        m_Phase = PLMovePhase.PL_MOVE_EXECUTE;

                        // 移動グリッドを求める
                        movePathList = StageGrid.instance.extractDepart2DestGoalGridIndexs( departGridIndex, destIndex );

                        return;
                    }
                }
                break;
                case PLMovePhase.PL_MOVE_EXECUTE:
                break;
                case PLMovePhase.PL_MOVE_END:
                break;
        }
    }

    public override void Exit()
    {


        base.Exit();
    }

    
}
