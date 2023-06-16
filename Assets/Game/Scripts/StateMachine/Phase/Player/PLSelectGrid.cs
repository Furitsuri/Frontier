using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PLSelectGrid : PhaseStateBase
{
    override public void Init()
    {
        base.Init();

        // グリッド選択を有効化
        BattleUISystem.Instance.ToggleSelectGrid( true );
    }

    override public bool Update()
    {
        // グリッド選択より遷移が戻ることはないため基底の更新は行わない
        // if( base.Update() ) { return true; }

        // グリッドの操作
        StageGrid.Instance.OperateCurrentGrid();

        // 現在の選択グリッド上に未行動のプレイヤーが存在する場合は行動選択へ
        int selectCharaIndex = StageGrid.Instance.GetCurrentGridInfo().charaIndex;

        var player = BattleManager.instance.SearchPlayerFromCharaIndex(selectCharaIndex);
        if ( player != null && !player.tmpParam.isEndCommand[(int)Character.BaseCommand.COMMAND_WAIT])
        {
            if (Input.GetKeyUp(KeyCode.Space))
            {
                TransitIndex = 0;   // 遷移

                return true;
            }
        }

        return false;
    }

    override public void Exit()
    {
        // グリッド選択を無効化 → 無効化しないほうが見た目がよかったため、コメントアウト
        // BattleUISystem.Instance.ToggleSelectGrid( false );

        base.Exit();
    }
}