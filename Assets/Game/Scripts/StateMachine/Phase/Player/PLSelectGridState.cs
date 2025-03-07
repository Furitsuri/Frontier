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

            
            // キーガイドを登録
            _inputFcd.RegisterInputCodes(
                ( GuideIcon.ALL_CURSOR, "Move",     CanAcceptInputDefault,  DetectOperateCursor,        DIRECTION_INPUT_INTERVAL),
                ( GuideIcon.CONFIRM,    "Command",  CanAcceptInputCommand,  DetectTransitCommandInput,  0.0f),
                ( GuideIcon.ESCAPE,     "TURN END", CanAcceptInputDefault,  DetectTransitTurnEndInput,  0.0f)
             );
        }

        override public bool Update()
        {
            // グリッド選択より遷移が戻ることはないため基底の更新は行わない
            // if( base.Update() ) { return true; }

            // 全てのキャラクターが待機済みになっていれば終了
            if( _btlRtnCtrl.BtlCharaCdr.IsEndAllArmyWaitCommand(Character.CHARACTER_TAG.PLAYER))
            {
                Back();

                return true;
            }

            Stage.GridInfo info;
            _stageCtrl.FetchCurrentGridInfo(out info);

            // 現在の選択グリッド上に未行動のプレイヤーが存在する場合は行動選択へ
            int selectCharaIndex = info.charaIndex;

            return ( 0 <= TransitIndex );
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
            if ( 0 <= TransitIndex )
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
        /// グリッドカーソルを操作します
        /// </summary>
        public bool DetectOperateCursor()
        {
            Constants.Direction direction = _inputFcd.GetInputDirection();

            if( _stageCtrl.OperateGridCursor( direction ) )
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// キャラクターコマンドの遷移へ移る際のコールバック関数です
        /// </summary>
        public bool DetectTransitCommandInput()
        {
            if( _inputFcd.GetInputConfirm() )
            {
                TransitIndex = (int)TransitTag.CharacterCommand;

                return true;
            }

            return false;
        }

        /// <summary>
        /// ターン終了遷移へ移る際のコールバック関数です
        /// </summary>
        /// <returns>ターン終了の成否</returns>
        public bool DetectTransitTurnEndInput()
        {
            // ターン終了確認へ遷移
            if( _inputFcd.GetInputOptions() )
            {
                TransitIndex = (int)TransitTag.TurnEnd;

                return true;
            }

            return false;
        }
    }
}