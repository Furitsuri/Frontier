using Frontier.Entities;
using Frontier.Stage;
using static Constants;

namespace Frontier.StateMachine
{
    public class PlSelectTileState : PlPhaseStateBase
    {
        private enum TransitTag
        {
            CHARACTER_COMMAND = 0,
            TURN_END,
        }

        private InputCodeStringWrapper _confirmStrWrapper;
        private string[] _confirmStrings;

        /// <summary>
        /// 遷移先を示すタグ
        /// </summary>
        override public void Init()
        {
            base.Init();

            // グリッド選択を有効化
            _stageCtrl.SetGridCursorControllerActive(true);

            // Confirmアイコンの文字列を設定
            _confirmStrings = new string[( int ) CHARACTER_TAG.NUM]
            {
                "COMMAND",      // PLAYER
                "SHOW RANGE",   // ENEMY
                "SHOW RANGE",   // OTHER
            };
            _confirmStrWrapper = new InputCodeStringWrapper( _confirmStrings[0] );
        }

        override public bool Update()
        {
            // グリッド選択より遷移が戻ることはないため基底の更新は行わない
            // if( base.Update() ) { return true; }

            // 全てのキャラクターが待機済みになっていれば終了
            if( _btlRtnCtrl.BtlCharaCdr.IsEndAllArmyWaitCommand( CHARACTER_TAG.PLAYER ) )
            {
                Back();

                return true;
            }

            TileDynamicData tileData = _stageCtrl.TileDataHdlr().GetCurrentTileDatas().Item2;
            if( tileData.CharaKey.IsValid() )
            {
                // Confirmアイコンの文字列を更新
                _confirmStrWrapper.Explanation = _confirmStrings[( int ) tileData.CharaKey.CharacterTag];
            }

            return ( 0 <= TransitIndex );
        }

        /// <summary>
        /// 入力コードを登録します
        /// </summary>
        override public void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            _inputFcd.RegisterInputCodes(
               (GuideIcon.ALL_CURSOR,   "MOVE",             CanAcceptDefault, new AcceptDirectionInput(AcceptDirection), GRID_DIRECTION_INPUT_INTERVAL, hashCode),
               (GuideIcon.CONFIRM,      _confirmStrWrapper, CanAcceptConfirm, new AcceptBooleanInput(AcceptConfirm), 0.0f, hashCode),
               (GuideIcon.OPT2,         "TURN END",         CanAcceptDefault, new AcceptBooleanInput(AcceptOptional), 0.0f, hashCode)
            );
        }

        /// <summary>
        /// キャラクターコマンドへ遷移可能かを判定します
        /// </summary>
        /// <returns>コマンド選択が可能か</returns>
        override protected bool CanAcceptConfirm()
        {
            if (0 <= TransitIndex)
            {
                return false;
            }

            Character character = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();
            if( null == character ) { return false; }

            // プレイヤーキャラクターの場合、行動終了状態でなければコマンド選択可能
            if( character.Params.CharacterParam.characterTag == CHARACTER_TAG.PLAYER )
            {
                return !character.Params.TmpParam.IsEndAction();
            }
            // 敵キャラクター、その他のキャラクターの場合、レンジ表示を行うため常に選択可能
            else
            {
                return true;
            }
        }

        /// <summary>
        /// 方向入力を受け取った際の処理を行います
        /// </summary>
        /// <param name="dir">方向入力</param>
        /// <returns>入力実行の有無</returns>
        override protected bool AcceptDirection(Direction dir)
        {
            return _stageCtrl.OperateGridCursorController( dir );
        }

        /// <summary>
        /// 決定入力を受けた際の処理を行います
        /// </summary>
        /// <param name="isConfirm">決定入力</param>
        /// <returns>入力実行の有無</returns>
        override protected bool AcceptConfirm(bool isInput)
        {
            if (!isInput) return false;

            Character character = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();
            if( null == character ) { return false; }

            // プレイヤーキャラクターの場合、行動終了状態でなければコマンド選択可能
            if( character.Params.CharacterParam.characterTag == CHARACTER_TAG.PLAYER )
            {
                TransitIndex = ( int ) TransitTag.CHARACTER_COMMAND;
                // コマンドを開くことをチュートリアルへ通知
                TutorialFacade.Notify( TutorialFacade.TriggerType.OpenBattleCommand );
            }
            // 敵キャラクター、その他のキャラクターの場合、攻撃範囲表示を行う
            else
            {
                Npc npc = character as Npc;
                if( null == npc ) { return false; }

                npc.ToggleAttackableRangeDisplay(); // 攻撃範囲表示を切り替える
            }

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

            TransitIndex = (int)TransitTag.TURN_END;

            return true;
        }
    }
}