using Frontier.Entities;
using System.Collections.Generic;
using UnityEngine;
using static Constants;

namespace Frontier
{
    /// <summary>
    /// プレイヤーターン終了確認の選択画面
    /// </summary>
    public class PlConfirmTurnEnd : PlPhaseStateBase
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

            _cmdIdxVal = new CommandList.CommandIndexedValue(1, 1);

            List<int> commandIndexs = new List<int>((int)ConfirmTag.NUM);
            for (int i = 0; i < (int)ConfirmTag.NUM; ++i)
            {
                commandIndexs.Add(i);
            }
            _commandList.Init(ref commandIndexs, CommandList.CommandDirection.HORIZONTAL, true, _cmdIdxVal);

            // 入力ガイドを登録
            _inputFcd.RegisterInputCodes<Constants.Direction>(
                ( ( GuideIcon.HORIZONTAL_CURSOR, "Select", CanAcceptInputDefault, DIRECTION_INPUT_INTERVAL), AcceptDirectionInput )
            );
            _inputFcd.RegisterInputCodes<bool>(
                ( (GuideIcon.CONFIRM, "Confirm", CanAcceptInputDefault, 0.0f), AcceptConfirmInput ),
                ( (GuideIcon.CANCEL, "Back", CanAcceptInputDefault, 0.0f), AcceptRevertInput )
            );

            _uiSystem.BattleUi.ToggleConfirmTurnEnd(true);
        }

        override public bool Update()
        {
            if (base.Update())
            {
                return true;
            }

            _uiSystem.BattleUi.ApplyTestColor2ConfirmTurnEndUI(_commandList.GetCurrentValue());

            return IsBack();
        }

        override public void Exit()
        {
            _uiSystem.BattleUi.ToggleConfirmTurnEnd(false);

            base.Exit();
        }

        /// <summary>
        /// 方向入力を受け取り、コマンドリストを操作させます
        /// </summary>
        /// <param name="dir">方向入力</param>
        /// <returns>入力によってリストカーソルの位置が更新されたか</returns>
        protected override bool AcceptDirectionInput(Direction dir)
        {
            return _commandList.OperateListCursor(dir);
        }

        /// <summary>
        /// 次のステートに遷移します
        /// </summary>
        private bool DetectConfirmInput()
        {
            if (_inputFcd.GetInputConfirm())
            {
                if (_commandList.GetCurrentValue() == (int)ConfirmTag.YES)
                {
                    // 全てのキャラクターを待機済みに設定して敵のフェーズに移行させる
                    _btlRtnCtrl.BtlCharaCdr.ApplyAllArmyEndAction(Character.CHARACTER_TAG.PLAYER);
                }

                Back();

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

            if (_commandList.GetCurrentValue() == (int)ConfirmTag.YES)
            {
                // 全てのキャラクターを待機済みに設定して敵のフェーズに移行させる
                _btlRtnCtrl.BtlCharaCdr.ApplyAllArmyEndAction(Character.CHARACTER_TAG.PLAYER);
            }

            Back();

            return true;
        }
    }
}