using Frontier.Entities;
using Frontier.Stage;
using System;
using Zenject;
using static Constants;

namespace Frontier.StateMachine
{
    public class PlSelectTileState : PlPhaseStateBase
    {
        private enum TransitTag
        {
            CHARACTER_COMMAND = 0,
            CHARACTER_STATUS,
            TURN_END,
        }

        private bool _isShowingAllDangerRange;  // 全危険範囲表示中かどうか
        private string[] _inputConfirmStrings;
        private string[] _inputToolStrings;
        private InputCodeStringWrapper _inputConfirmStrWrapper;
        private InputCodeStringWrapper _inputToolStrWrapper;

        /// <summary>
        /// 遷移先を示すタグ
        /// </summary>
        override public void Init()
        {
            base.Init();

            _isShowingAllDangerRange　= false;
            _stageCtrl.SetGridCursorControllerActive( true );   // グリッド選択を有効化

            // Confirmアイコンの文字列を設定
            _inputConfirmStrings = new string[( int ) CHARACTER_TAG.NUM]
            {
                "COMMAND",          // PLAYER
                "TOGGLE RANGE",     // ENEMY
                "TOGGLE RANGE",     // OTHER
            };
            // TOOLアイコンの文字列を設定
            _inputToolStrings = new string[]
            {
                "SHOW DANGER RANGE", // 危険領域表示
                "HIDE DANGER RANGE", // 危険領域非表示
            };

            _inputConfirmStrWrapper = new InputCodeStringWrapper( _inputConfirmStrings[0] );
            _inputToolStrWrapper    = new InputCodeStringWrapper( _inputToolStrings[0] );
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
                _inputConfirmStrWrapper.Explanation = _inputConfirmStrings[( int ) tileData.CharaKey.CharacterTag];
            }

            // TOOLアイコンの文字列を更新
            _inputToolStrWrapper.Explanation = _isShowingAllDangerRange ? _inputToolStrings[1] : _inputToolStrings[0];

            return ( 0 <= TransitIndex );
        }

        /// <summary>
        /// 入力コードを登録します
        /// </summary>
        override public void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            _inputFcd.RegisterInputCodes(
               (GuideIcon.ALL_CURSOR, "MOVE", CanAcceptDefault, new AcceptDirectionInput( AcceptDirection ), GRID_DIRECTION_INPUT_INTERVAL, hashCode),
               (GuideIcon.CONFIRM, _inputConfirmStrWrapper, CanAcceptConfirm, new AcceptBooleanInput( AcceptConfirm ), 0.0f, hashCode),
               (GuideIcon.TOOL, _inputToolStrWrapper, CanAcceptDefault, new AcceptBooleanInput( AcceptTool ), 0.0f, hashCode),
               (GuideIcon.INFO, "STATUS", CanAcceptInfo, new AcceptBooleanInput( AcceptInfo ), 0.0f, hashCode),
               (GuideIcon.OPT2, "TURN END", CanAcceptDefault, new AcceptBooleanInput( AcceptOptional ), 0.0f, hashCode)
            );
        }

        /// <summary>
        /// キャラクターコマンドへ遷移可能かを判定します
        /// </summary>
        /// <returns>コマンド選択が可能か</returns>
        override protected bool CanAcceptConfirm()
        {
            if( 0 <= TransitIndex )
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
        /// グリッド上にキャラクターが存在していればステータス画面に遷移可能と判定します
        /// </summary>
        /// <returns></returns>
        override protected bool CanAcceptInfo()
        {
            return null != _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();
        }

        /// <summary>
        /// 方向入力を受け取り、選択グリッドを操作します
        /// </summary>
        /// <param name="dir">方向入力</param>
        /// <returns>入力実行の有無</returns>
        override protected bool AcceptDirection( Direction dir )
        {
            return _stageCtrl.OperateGridCursorController( dir );
        }

        /// <summary>
        /// 決定入力を受けた際の各種処理を行います
        /// </summary>
        /// <param name="isConfirm">決定入力</param>
        /// <returns>入力実行の有無</returns>
        override protected bool AcceptConfirm( bool isInput )
        {
            if( !isInput ) { return false; }

            Character character = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();
            if( null == character ) { return false; }

            // プレイヤーキャラクターの場合、行動終了状態でなければコマンド選択可能
            if( character.Params.CharacterParam.characterTag == CHARACTER_TAG.PLAYER )
            {
                TransitStateWithExit( ( int ) TransitTag.CHARACTER_COMMAND );
                // コマンドを開くことをチュートリアルへ通知
                TutorialFacade.Notify( TutorialFacade.TriggerType.OpenBattleCommand );
            }
            // 敵キャラクター、その他のキャラクターの場合、攻撃範囲表示を行う
            else
            {
                Npc npc = character as Npc;
                if( null == npc ) { return false; }

                npc.ToggleAttackableRangeDisplay(); // 攻撃範囲表示を切り替える

                // 全ての敵及び第三勢力の攻撃範囲表示状態を確認し、全てが_isShowingAllDangerRangeの値と異なっている場合は、全危険範囲表示状態を切り替える
                bool isAllMismatch = true;
                foreach( Npc npcChara in _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable( CHARACTER_TAG.ENEMY, CHARACTER_TAG.OTHER ) )
                {
                    if( _isShowingAllDangerRange == npcChara.ActionRangeCtrl.ActionableRangeRdr.IsShowingAttackableRange )
                    {
                        isAllMismatch = false;
                        break;
                    }
                }

                if( isAllMismatch ) { _isShowingAllDangerRange = !_isShowingAllDangerRange; }
            }

            return true;
        }

        /// <summary>
        /// OPTION入力を受けた際にターン終了へ遷移させます
        /// </summary>
        /// <param name="isOptional"></param>
        /// <returns>入力実行の有無</returns>
        override protected bool AcceptOptional( bool isOptional )
        {
            if( !isOptional ) return false;

            TransitState( ( int ) TransitTag.TURN_END );

            return true;
        }

        /// <summary>
        /// TOOL入力を受けた際に敵・その他キャラクターの攻撃可能範囲表示を切り替えます
        /// </summary>
        /// <param name="isInput"></param>
        /// <returns></returns>
        override protected bool AcceptTool( bool isInput )
        {
            if( !isInput ) { return false; }

            _isShowingAllDangerRange = !_isShowingAllDangerRange;

            foreach( Npc npcChara in _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable( CHARACTER_TAG.ENEMY, CHARACTER_TAG.OTHER ) )
            {
                npcChara.SetAttackableRangeDisplay( _isShowingAllDangerRange );
            }

            return true;
        }

        /// <summary>
        /// グリッド上のキャラクターのステータス画面を開きます
        /// </summary>
        /// <param name="isInput"></param>
        /// <returns></returns>
        override protected bool AcceptInfo( bool isInput )
        {
            if( !isInput ) { return false; }

            Handler.ReceiveContext( _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter() );

            TransitState( ( int ) TransitTag.CHARACTER_STATUS );

            return true;
        }
    }
}