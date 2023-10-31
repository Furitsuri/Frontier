using Frontier.Stage;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier
{
    public class PLConfirmTurnEnd : PhaseStateBase
    {
        enum ConfirmTag
        {
            YES = 0,
            NO,

            NUM
        }

        private CommandList _commandList = new CommandList();

        override public void Init(BattleManager btlMgr, StageController stgCtrl)
        {
            base.Init(btlMgr, stgCtrl);

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
            BattleUISystem.Instance.ApplyTestColor2ConfirmTurnEndUI(_commandList.GetCurrentValue());

            if (Input.GetKeyUp(KeyCode.Space))
            {
                if (_commandList.GetCurrentValue() == (int)ConfirmTag.YES)
                {
                    // 全てのキャラクターを待機済みに設定して敵のフェーズに移行させる
                    _btlMgr.ApplyAllPlayerWaitEnd();
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
}