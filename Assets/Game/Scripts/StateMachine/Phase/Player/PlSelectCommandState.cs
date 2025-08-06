﻿using Frontier.Combat;
using Frontier.Entities;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;
using static Constants;

namespace Frontier
{
    public class PlSelectCommandState : PlPhaseStateBase
    {
        private CommandList _commandList = new CommandList();
        private CommandList.CommandIndexedValue _cmdIdxVal;

        /// <summary>
        /// 入力情報を初期化します
        /// </summary>
        private void InitInputInfo()
        {
            _cmdIdxVal = new CommandList.CommandIndexedValue(0, 0);

            // UI側へこのスクリプトを登録し、UIを表示
            List<Command.COMMAND_TAG> executableCommands;
            _selectPlayer.FetchExecutableCommand(out executableCommands, _stageCtrl);

            // 入力ベース情報の設定
            List<int> commandIndexs = new List<int>();
            foreach (var executableCmd in executableCommands)
            {
                commandIndexs.Add((int)executableCmd);
            }
            _commandList.Init(ref commandIndexs, CommandList.CommandDirection.VERTICAL, false, _cmdIdxVal);

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
            _selectPlayer = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter() as Player;
            NullCheck.AssertNotNull(_selectPlayer);

            // 可能な行動が全て終了している場合は終了
            if (_selectPlayer.IsEndAction())
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
            if( _selectPlayer.IsEndAction() )
            {
                Back();
                return true;
            }

            if (base.Update())
            {
                // 移動のみ終了している場合は移動前に戻れるように          
                if (_selectPlayer.IsEndCommand(Command.COMMAND_TAG.MOVE) && !_selectPlayer.IsEndCommand(Command.COMMAND_TAG.ATTACK))
                {
                    _stageCtrl.FollowFootprint(_selectPlayer);
                    _stageCtrl.UpdateGridInfo();
                    _selectPlayer.SetEndCommandStatus(Command.COMMAND_TAG.MOVE, false );
                }

                return true;
            }

            return (0 <= TransitIndex);
        }

        /// <summary>
        /// 現在のステートから離脱します
        /// </summary>
        override public void ExitState()
        {
            // UIを非表示
            _uiSystem.BattleUi.TogglePLCommand(false);

            base.ExitState();
        }

        /// <summary>
        /// 入力コードを登録します
        /// </summary>
        override public void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            _inputFcd.RegisterInputCodes(
               (new GuideIcon[] { GuideIcon.VERTICAL_CURSOR },  "Select",   CanAcceptDefault, new IAcceptInputBase[] { new AcceptDirectionInput(AcceptDirection) }, MENU_DIRECTION_INPUT_INTERVAL, hashCode),
               (new GuideIcon[] { GuideIcon.CONFIRM },          "Confirm",  CanAcceptDefault, new IAcceptInputBase[] { new AcceptBooleanInput(AcceptConfirm) }, 0.0f, hashCode),
               (new GuideIcon[] { GuideIcon.CANCEL },           "Back",     CanAcceptDefault, new IAcceptInputBase[] { new AcceptBooleanInput(AcceptCancel) }, 0.0f, hashCode)
            );
        }

        /// <summary>
        /// 決定入力を受けた際の処理を行います
        /// </summary>
        /// <param name="isConfirm">決定入力</param>
        /// <returns>入力実行の有無</returns>
        override protected bool AcceptDirection(Constants.Direction dir)
        {
            return _commandList.OperateListCursor(dir);
        }

        /// <summary>
        /// 決定入力を受けた際の処理を行います
        /// </summary>
        /// <param name="isConfirm">決定入力</param>
        /// /// <returns>決定入力の有無</returns>
        override protected bool AcceptConfirm( bool isInput )
        {
            if( !isInput ) return false;

            TransitIndex = GetCommandValue();

            return true;
        }

        /// <summary>
        /// キャンセル入力を受けた際の処理を行います
        /// </summary>
        /// <param name="isCancel">キャンセル入力の有無</param>
        override protected bool AcceptCancel(bool isCancel)
        {
            if( base.AcceptCancel(isCancel) )
            {
                // 以前の状態に巻き戻せる場合は状態を巻き戻す
                if (_selectPlayer.IsRewindStatePossible())
                {
                    Rewind();
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// 現在のコマンドのIndex値を取得します
        /// </summary>
        /// <returns>コマンドのIndex値</returns>
        public int GetCommandIndex()
        {
            return _cmdIdxVal.index;
        }

        /// <summary>
        /// 現在のコマンドのValue値を取得します
        /// </summary>
        /// <returns>コマンドのValue値</returns>
        public int GetCommandValue()
        {
            return _cmdIdxVal.value;
        }
    }
}