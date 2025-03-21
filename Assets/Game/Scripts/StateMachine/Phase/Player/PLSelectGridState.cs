using Frontier.Entities;
using UnityEngine;
using static Constants;

namespace Frontier
{
    public class PlSelectGridState : PlPhaseStateBase
    {
        /// <summary>
        /// 遷移先を示すタグ
        /// </summary>
        enum TransitTag
        {
            CharacterCommand = 0,
            TurnEnd,
        }

        override public void Init()
        {
            base.Init();

            // グリッド選択を有効化
            _stageCtrl.SetGridCursorActive(true);

            // 入力ガイドを登録
            _inputFcd.RegisterInputCodes(
               (GuideIcon.ALL_CURSOR,   "Move", CanAcceptDefault,      new AcceptDirectionInput(AcceptDirection),  DIRECTION_INPUT_INTERVAL),
               (GuideIcon.CONFIRM,      "Command", CanAcceptConfirm,   new AcceptBooleanInput(AcceptConfirm),      0.0f),
               (GuideIcon.ESCAPE,       "TURN END", CanAcceptDefault,  new AcceptBooleanInput(AcceptOptional),     0.0f)
            );
        }

        override public bool Update()
        {
            // グリッド選択より遷移が戻ることはないため基底の更新は行わない
            // if( base.Update() ) { return true; }

            // 全てのキャラクターが待機済みになっていれば終了
            if (_btlRtnCtrl.BtlCharaCdr.IsEndAllArmyWaitCommand(Character.CHARACTER_TAG.PLAYER))
            {
                Back();

                return true;
            }

            Stage.GridInfo info;
            _stageCtrl.FetchCurrentGridInfo(out info);

            // 現在の選択グリッド上に未行動のプレイヤーが存在する場合は行動選択へ
            int selectCharaIndex = info.charaIndex;

            return (0 <= TransitIndex);
        }

        /// <summary>
        /// キャラクターコマンド遷移へ移る際のコールバック関数
        /// </summary>
        /// <returns>コマンド選択が可能か</returns>
        override protected bool CanAcceptConfirm()
        {
            if (0 <= TransitIndex)
            {
                return false;
            }

            Character character = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();
            if (character != null &&
                 character.param.characterTag == Character.CHARACTER_TAG.PLAYER &&
                 !character.IsEndAction())
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 方向入力を受け取った際の処理を行います
        /// </summary>
        /// <param name="dir">方向入力</param>
        /// <returns>入力実行の有無</returns>
        override protected bool AcceptDirection(Direction dir)
        {
            return _stageCtrl.OperateGridCursor(dir);
        }

        /// <summary>
        /// 決定入力を受けた際の処理を行います
        /// </summary>
        /// <param name="isConfirm">決定入力</param>
        /// <returns>入力実行の有無</returns>
        override protected bool AcceptConfirm(bool isInput)
        {
            if (!isInput) return false;

            TransitIndex = (int)TransitTag.CharacterCommand;

            return true;
        }

        /// <summary>
        /// オプション入力を受けた際の処理を行います
        /// </summary>
        /// <param name="isOptional">オプション入力</param>
        /// <returns>入力実行の有無</returns>
        override protected bool AcceptOptional(bool isOptional)
        {
            if (!isOptional) return false;

            TransitIndex = (int)TransitTag.TurnEnd;

            return true;
        }
    }
}