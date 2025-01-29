using Frontier.Entities;

namespace Frontier
{
    public class PLSelectGridState : PhaseStateBase
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
            SetInputGuides(
                (Constants.KeyIcon.ALL_CURSOR,  "Move",     null),
                (Constants.KeyIcon.ESCAPE,      "TURN END", TransitConfirmTurnEndCallBack));
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

            // TODO : キーマネージャ側に操作処理を完全に移す試験のため、一旦コメントアウト
            /*
            // ターン終了確認へ遷移
            if (Input.GetKeyUp(KeyCode.Escape))
            {
                TransitIndex = (int)TransitTag.TurnEnd;
                return true;
            }
            */

            // グリッドの操作
            _stageCtrl.OperateGridCursor();
            Stage.GridInfo info;
            _stageCtrl.FetchCurrentGridInfo(out info);

            // 現在の選択グリッド上に未行動のプレイヤーが存在する場合は行動選択へ
            int selectCharaIndex = info.charaIndex;

            // TODO : キーマネージャ側に操作処理を完全に移す試験のため、一旦コメントアウト
            /*
            Character character = _btlRtnCtrl.GetSelectCharacter();
            if (character != null &&
                 character.param.characterTag == Character.CHARACTER_TAG.PLAYER &&
                 !character.tmpParam.isEndCommand[(int)Character.Command.COMMAND_TAG.WAIT])
            {
                if (Input.GetKeyUp(KeyCode.Space))
                {
                    TransitIndex = (int)TransitTag.CharacterCommand;

                    return true;
                }
            }
            */

            return ( 0 <= TransitIndex );
        }

        override public void Exit()
        {
            // グリッド選択を無効化 → TODO : 無効化しないほうがゲーム実行時における見た目がよかったため、一旦コメントアウトで保留
            // _stageCtrl.SetGridCursorActive( false );

            base.Exit();
        }

        public void OperateGridCursorCallBack()
        {

            _stageCtrl.OperateGridCursor(Constants.Direction.LEFT);
        }

        /// <summary>
        /// キャラクターコマンド遷移へ移る際のコールバック関数
        /// </summary>
        /// <returns></returns>
        public bool TransitCharacterCommandCallBack()
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
                TransitIndex = (int)TransitTag.CharacterCommand;

                return true;
            }

            return false;
        }

        /// <summary>
        /// ターン終了遷移へ移る際のコールバック関数
        /// </summary>
        /// <returns></returns>
        public bool TransitConfirmTurnEndCallBack()
        {
            if( 0 <= TransitIndex )
            {
                return false;
            }

            TransitIndex = (int)TransitTag.TurnEnd;

            return true;
        }
    }
}