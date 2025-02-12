using Frontier.Entities;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier
{
    public class PlConfirmTurnEnd : PhaseStateBase
    {
        enum ConfirmTag
        {
            YES = 0,
            NO,

            NUM
        }

        private CommandList _commandList = new CommandList();
        private CommandList.CommandIndexedValue _cmdIdxVal;

        override public void Init()
        {
            base.Init();

            _cmdIdxVal = new CommandList.CommandIndexedValue( 0, 0 );

            List<int> commandIndexs = new List<int>((int)ConfirmTag.NUM);
            for (int i = 0; i < (int)ConfirmTag.NUM; ++i)
            {
                commandIndexs.Add(i);
            }
            _commandList.Init(ref commandIndexs, CommandList.CommandDirection.HORIZONTAL, _cmdIdxVal);

            _uiSystem.BattleUi.ToggleConfirmTurnEnd(true);
        }

        override public bool Update()
        {
            if (base.Update())
            {
                return true;
            }

            _commandList.Update();
            _uiSystem.BattleUi.ApplyTestColor2ConfirmTurnEndUI(_commandList.GetCurrentValue());

            if (Input.GetKeyUp(KeyCode.Space))
            {
                if (_commandList.GetCurrentValue() == (int)ConfirmTag.YES)
                {
                    // 全てのキャラクターを待機済みに設定して敵のフェーズに移行させる
                    _btlRtnCtrl.BtlCharaCdr.ApplyAllArmyWaitEnd(Character.CHARACTER_TAG.PLAYER);
                }

                Back();

                return true;
            }


            return false;
        }

        override public void Exit()
        {
            _uiSystem.BattleUi.ToggleConfirmTurnEnd(false);

            base.Exit();
        }
    }
}