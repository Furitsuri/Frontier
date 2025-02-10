using Frontier.Entities;
using UnityEngine;
using static Constants;

namespace Frontier
{
    public class PlSelectGridState : PhaseStateBase
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
                ( GuideIcon.ALL_CURSOR,  "Move",        InputFacade.Enable,             _stageCtrl.OperateGridCursor,       0.0f),
                ( GuideIcon.DECISION,    "Command",     EnableCharacterCommandCallBack, TransitCharacterCommandCallback,    0.0f),
                ( GuideIcon.ESCAPE,      "TURN END",    InputFacade.Enable,             TransitConfirmTurnEndCallBack,      0.0f)
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
        /// <returns></returns>
        public bool EnableCharacterCommandCallBack()
        {
            if ( 0 <= TransitIndex )
            {
                return false;
            }

            Character character = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();
            if (character != null &&
                 character.param.characterTag == Character.CHARACTER_TAG.PLAYER &&
                 !character.tmpParam.isEndCommand[(int)Character.Command.COMMAND_TAG.WAIT])
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        public void TransitCharacterCommandCallback()
        {
            if( Input.GetKeyUp(KeyCode.Space) )
            {
                TransitIndex = (int)TransitTag.CharacterCommand;
            }
        }

        /// <summary>
        /// ターン終了遷移へ移る際のコールバック関数
        /// </summary>
        /// <returns>ターン終了の成否</returns>
        public void TransitConfirmTurnEndCallBack()
        {
            // ターン終了確認へ遷移
            if (Input.GetKeyUp(KeyCode.Escape))
            {
                TransitIndex = (int)TransitTag.TurnEnd;
            }
        }
    }
}