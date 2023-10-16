using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;
using static Frontier.Character;

namespace Frontier
{
    public class PLSelectCommandState : PhaseStateBase
    {
        public int SelectCommandIndex { get; private set; } = 0;
        public int SelectCommandValue { get; private set; } = 0;
        private Player _selectPlayer;
        private CommandList _commandList = new CommandList();

        override public void Init()
        {
            base.Init();

            // 選択中のプレイヤーを取得
            _selectPlayer = (Player)_btlMgr.GetSelectCharacter();
            if (_selectPlayer == null)
            {
                Debug.Assert(false);

                return;
            }

            var endCommand = _selectPlayer.tmpParam.isEndCommand;
            if (endCommand[(int)Command.COMMAND_TAG.MOVE] && endCommand[(int)Command.COMMAND_TAG.ATTACK])
            {
                return;
            }

            // UI側へこのスクリプトを登録し、UIを表示
            var instance = BattleUISystem.Instance;
            List<Character.Command.COMMAND_TAG> executableCommands;
            _selectPlayer.FetchExecutableCommand(out executableCommands);

            // 入力ベース情報の設定
            List<int> commandIndexs = new List<int>();
            foreach ( var executableCmd in executableCommands )
            {
                commandIndexs.Add((int)executableCmd);
            }
            _commandList.Init(ref commandIndexs, CommandList.CommandDirection.VERTICAL);
            SelectCommandIndex = _commandList.GetCurrentIndex();
            SelectCommandValue = _commandList.GetCurrentValue();

            instance.PLCommandWindow.RegistPLCommandScript(this);
            instance.PLCommandWindow.SetExecutableCommandList(executableCommands);
            instance.TogglePLCommand(true);
        }

        override public bool Update()
        {
            var endCommand = _selectPlayer.tmpParam.isEndCommand;

            // 移動と攻撃が終わっている場合は自動的に終了に
            if (endCommand[(int)Command.COMMAND_TAG.MOVE] && endCommand[(int)Command.COMMAND_TAG.ATTACK])
            {
                Back();
                // 待機を終わらせる
                endCommand[(int)Command.COMMAND_TAG.WAIT] = true;

                return true;
            }

            if (base.Update())
            {
                // 移動のみ終了している場合は移動前に戻れるように          
                if (endCommand[(int)Command.COMMAND_TAG.MOVE] && !endCommand[(int)Command.COMMAND_TAG.ATTACK])
                {
                    Stage.StageController.Instance.FollowFootprint(_selectPlayer);
                    Stage.StageController.Instance.UpdateGridInfo();
                    endCommand[(int)Command.COMMAND_TAG.MOVE] = false;
                }

                return true;
            }

            if (_commandList.Update())
            {
                SelectCommandIndex = _commandList.GetCurrentIndex();
                SelectCommandValue = _commandList.GetCurrentValue();
            }

            if (Input.GetKeyUp(KeyCode.Space))
            {
                TransitIndex = SelectCommandValue;

                return true;
            }

            return false;
        }

        override public void Exit()
        {
            // UIを非表示
            BattleUISystem.Instance.TogglePLCommand(false);

            base.Exit();
        }
    }
}
