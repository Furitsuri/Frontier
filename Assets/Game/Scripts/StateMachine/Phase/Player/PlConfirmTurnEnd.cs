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

            // キーガイドを登録
            _inputFcd.RegisterInputCodes(
                (GuideIcon.HORIZONTAL_CURSOR,   "Select",   CanAcceptInputDefault,     _commandList.UpdateInput,   0.0f),
                (GuideIcon.CONFIRM,             "Decision", CanAcceptInputDefault,     DetectTransitionInput,      0.0f),
                (GuideIcon.CANCEL,              "Back",     CanAcceptInputDefault,     DetectRevertInput,          0.0f)
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
        /// 次のステートに遷移します
        /// </summary>
        private bool DetectTransitionInput()
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
    }
}