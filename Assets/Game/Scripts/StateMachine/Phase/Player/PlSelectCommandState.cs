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
            List<Character.Command.COMMAND_TAG> executableCommands;
            _selectPlayer.FetchExecutableCommand(out executableCommands, _stageCtrl);

            // 入力ベース情報の設定
            List<int> commandIndexs = new List<int>();
            foreach (var executableCmd in executableCommands)
            {
                commandIndexs.Add((int)executableCmd);
            }
            _commandList.Init(ref commandIndexs, CommandList.CommandDirection.VERTICAL, false, _cmdIdxVal);

            // 入力ガイドを登録
            _inputFcd.RegisterInputCodes<Constants.Direction>(
                ((GuideIcon.VERTICAL_CURSOR, "Select", CanAcceptInputDefault, DIRECTION_INPUT_INTERVAL), AcceptDirectionInput)
            );
            _inputFcd.RegisterInputCodes<bool>(
                ( (GuideIcon.CONFIRM,   "Confirm", CanAcceptInputDefault, 0.0f),    AcceptConfirmInput),
                ( (GuideIcon.CANCEL,    "Back", CanAcceptInputDefault, 0.0f),     AcceptRevertInput)
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
            _selectPlayer = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter() as Player;
            DebugUtils.NULL_ASSERT(_selectPlayer);

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
                if (_selectPlayer.IsEndCommand(Character.Command.COMMAND_TAG.MOVE) && !_selectPlayer.IsEndCommand(Character.Command.COMMAND_TAG.ATTACK))
                {
                    _stageCtrl.FollowFootprint(_selectPlayer);
                    _stageCtrl.UpdateGridInfo();
                    _selectPlayer.SetEndCommandStatus(Character.Command.COMMAND_TAG.MOVE, false );
                }

                return true;
            }

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
        /// 方向入力を受け取り、コマンドリストを操作させます
        /// </summary>
        /// <param name="dir">方向入力</param>
        /// <returns>入力によってリストカーソルの位置が更新されたか</returns>
        override protected bool AcceptDirectionInput(Constants.Direction dir)
        {
            return _commandList.OperateListCursor(dir);
        }

        /// <summary>
        /// 入力を検知して、以前のステートに遷移するフラグをONに切り替えます
        /// </summary>
        override protected bool DetectRevertInput()
        {
            if ( _inputFcd.GetInputCancel() )
            {
                Back();

                // 以前の状態に巻き戻せる場合は状態を巻き戻す
                if( _selectPlayer.IsRewindStatePossible() )
                {
                    Rewind();

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 次のステートに遷移します
        /// </summary>
        private bool DetectConfirmInput()
        {
            if ( _inputFcd.GetInputConfirm() )
            {
                TransitIndex = GetCommandValue();

                return true;
            }

            return false;
        }

        /// <summary>
        /// 決定入力を受けた際の処理を行います
        /// </summary>
        /// <param name="isConfirm">決定入力の有無</param>
        private bool AcceptConfirmInput( bool isConfirm )
        {
            if( !isConfirm ) return false;

            TransitIndex = GetCommandValue();

            return true;
        }

        /// <summary>
        /// キャンセル入力を受けた際の処理を行います
        /// </summary>
        /// <param name="isRevert">キャンセル入力の有無</param>
        protected override bool AcceptRevertInput(bool isRevert)
        {
            if( base.AcceptRevertInput(isRevert) )
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