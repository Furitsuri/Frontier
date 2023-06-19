using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PLConfirmTurnEnd : PhaseStateBase
{
    enum ConfirmTag
    {
        YES = 0,
        NO,

        NUM
    }

    private CommandList _commandList = new CommandList();

    override public void Init()
    {
        base.Init();

        List<int> commandIndexs = new List<int>((int)ConfirmTag.NUM);
        for (int i = 0; i < (int)ConfirmTag.NUM; ++i)
        {
            commandIndexs.Add(i);
        }
        _commandList.Init(ref commandIndexs, CommandList.CommandDirection.HORIZONTAL);

        BattleUISystem.Instance.ToggleConfirmTurnEnd(true);
    }

    override public bool Update()
    {
        if (base.Update())
        {
            return true;
        }

        _commandList.Update();
        BattleUISystem.Instance.ApplyTestColor2ConfirmTurnEndUI( _commandList.GetCurrentIndex() );

        if ( Input.GetKeyUp(KeyCode.Space) )
        {
            if( _commandList.GetCurrentIndex() == (int)ConfirmTag.YES )
            {
                // 全てのキャラクターを待機済みに設定して敵のフェーズに移行させる
                BattleManager.Instance.ApplyAllPlayerWaitEnd();
            }

            Back();

            return true;
        }


        return false;
    }

    override public void Exit()
    {
        BattleUISystem.Instance.ToggleConfirmTurnEnd(false);

        base.Exit();
    }
}
