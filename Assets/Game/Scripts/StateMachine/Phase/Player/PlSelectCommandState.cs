using Frontier.Entities;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;
using static Constants;

namespace Frontier
{
    public class PlSelectCommandState : PhaseStateBase
    {
        public int SelectCommandIndex { get; private set; } = 0;
        public int SelectCommandValue { get; private set; } = 0;
        private Player _selectPlayer;
        private CommandList _commandList = new CommandList();

        /// <summary>
        /// 入力情報を初期化します
        /// </summary>
        private void InitInputInfo()
        {
            // UI側へこのスクリプトを登録し、UIを表示
            List<Character.Command.COMMAND_TAG> executableCommands;
            _selectPlayer.FetchExecutableCommand(out executableCommands, _stageCtrl);

            // 入力ベース情報の設定
            List<int> commandIndexs = new List<int>();
            foreach (var executableCmd in executableCommands)
            {
                commandIndexs.Add((int)executableCmd);
            }
            _commandList.Init(ref commandIndexs, CommandList.CommandDirection.VERTICAL);
            SelectCommandIndex = _commandList.GetCurrentIndex();
            SelectCommandValue = _commandList.GetCurrentValue();

            // キーガイドを登録
            _inputFcd.RegisterInputCodes(
                (GuideIcon.VERTICAL_CURSOR, "Select",   InputFacade.Enable, _commandList.UpdateInput,   0.0f),
                (GuideIcon.DECISION,        "Decision", InputFacade.Enable, TransitNextState,           0.0f)
             );

            _uiSystem.BattleUi.PlCommandWindow.RegistPLCommandScript(this);
            _uiSystem.BattleUi.PlCommandWindow.SetExecutableCommandList(executableCommands);
            _uiSystem.BattleUi.TogglePLCommand(true);
        }

        /// <summary>
        /// 初期化します
        /// </summary>
        override public void Init()
        {
            base.Init();

            // 選択中のプレイヤーを取得
            _selectPlayer = (Player)_btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();
            DebugUtils.NULL_ASSERT(_selectPlayer);

            var endCommand = _selectPlayer.tmpParam.isEndCommand;
            if (endCommand[(int)Character.Command.COMMAND_TAG.MOVE] && endCommand[(int)Character.Command.COMMAND_TAG.ATTACK])
            {
                return;
            }

            InitInputInfo();
        }

        /// <summary>
        /// 更新します
        /// </summary>
        /// <returns>0以上の値のとき次の状態に遷移します</returns>
        override public bool Update()
        {
            bool isImpossibleCmd = _selectPlayer.tmpParam.isEndCommand[(int)Character.Command.COMMAND_TAG.WAIT];
            if( isImpossibleCmd )
            {
                Back();
                return true;
            }

            var endCommand = _selectPlayer.tmpParam.isEndCommand;

            if (base.Update())
            {
                // 移動のみ終了している場合は移動前に戻れるように          
                if (endCommand[(int)Character.Command.COMMAND_TAG.MOVE] && !endCommand[(int)Character.Command.COMMAND_TAG.ATTACK])
                {
                    _stageCtrl.FollowFootprint(_selectPlayer);
                    _stageCtrl.UpdateGridInfo();
                    endCommand[(int)Character.Command.COMMAND_TAG.MOVE] = false;
                }

                return true;
            }

            // 入力ガイドに_commandList.UpdateInputを登録しているため、IndexやValueは自動で更新される
            SelectCommandIndex = _commandList.GetCurrentIndex();
            SelectCommandValue = _commandList.GetCurrentValue();

            return (0 <= TransitIndex);
        }

        /// <summary>
        /// 現在のステートから離脱します
        /// </summary>
        override public void Exit()
        {
            // UIを非表示
            _uiSystem.BattleUi.TogglePLCommand(false);

            base.Exit();
        }

        /// <summary>
        /// 次のステートに遷移します
        /// </summary>
        public void TransitNextState()
        {
            if (Input.GetKeyUp(KeyCode.Space))
            {
                TransitIndex = SelectCommandValue;
            }
        }
    }
}