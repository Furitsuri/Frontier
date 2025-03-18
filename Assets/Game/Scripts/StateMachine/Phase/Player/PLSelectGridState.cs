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
            _inputFcd.RegisterInputCodes<Constants.Direction>(
                ((GuideIcon.ALL_CURSOR, "Move", CanAcceptInputDefault, DIRECTION_INPUT_INTERVAL), AcceptDirectionInput)
             );

            _inputFcd.RegisterInputCodes<bool>(
                ((GuideIcon.CONFIRM, "Command", CanAcceptInputCommand, 0.0f), AcceptConfirmInput),
                ((GuideIcon.ESCAPE, "TURN END", CanAcceptInputDefault, 0.0f), AcceptOptionalInput)
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
        /// 現状のステートから抜け出します
        /// </summary>
        override public void Exit()
        {
            // グリッド選択を無効化 → TODO : 無効化しないほうがゲーム実行時における見た目がよかったため、一旦コメントアウトで保留
            // _stageCtrl.SetGridCursorActive( false );

            base.Exit();
        }

        /// <summary>
        /// キャラクターコマンド遷移へ移る際のコールバック関数
        /// </summary>
        /// <returns>コマンド選択が可能か</returns>
        public bool CanAcceptInputCommand()
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
        /// 決定入力を受け取った際の処理を行います
        /// </summary>
        /// <param name="isConfirm">決定入力の有無</param>
        /// <returns>入力受付の是非</returns>
        public bool AcceptConfirmInput(bool isConfirm)
        {
            if (!isConfirm) return false;

            TransitIndex = (int)TransitTag.CharacterCommand;

            return true;
        }

        /// <summary>
        /// オプション入力を受け取った際の処理を行います
        /// </summary>
        /// <param name="isOptional">オプション入力の有無</param>
        /// <returns>入力受付の是非</returns>
        public bool AcceptOptionalInput(bool isOptional)
        {
            if (!isOptional) return false;

            TransitIndex = (int)TransitTag.TurnEnd;

            return true;
        }
    }
}