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

    override public void Update()
    {
        base.Update();

        // グリッドの操作
        StageGrid.instance.OperateCurrentGrid();

        // 現在の選択グリッド上に未行動のプレイヤーが存在する場合は行動選択へ
        int selectCharaIndex = StageGrid.instance.getCurrentGridInfo().charaIndex;

        var player = BattleManager.instance.GetPlayerFromIndex(selectCharaIndex);
        if ( player != null && !player.tmpParam.isEndCommand[(int)Character.BaseCommand.COMMAND_WAIT])
        {
            if (Input.GetKeyUp(KeyCode.Space))
            {
                TransitIndex = 0;   // 遷移
                return;
            }
        }
    }

    override public void Exit()
    {
        // グリッド選択を無効化
        // 無効化しないほうが見た目がよかったため、コメントアウト
        // BattleUISystem.Instance.ToggleSelectGrid( false );

        base.Exit();
    }
}