using Frontier.Combat;
using Frontier.Entities;
using Frontier.Stage;
using Frontier.Tutorial;
using Frontier.UI;
using Zenject;
using static Constants;

namespace Frontier.Battle
{
    public class PlSelectTileState : PlPhaseStateBase
    {
        private enum TransitTag
        {
            CHARACTER_COMMAND = 0,
            CHARACTER_STATUS,
            TURN_END,
            SELECT_RESERVED_ACTION,
            SELECT_TILE_MENU,
            GROUP_MOVE,
        }

        [Inject] protected GroupMoveRegistrationList _groupMoveRegistrationList = null;

        private bool _isShowingAllDangerRange;  // 全危険範囲表示中かどうか
        private bool _isWaitingForTileMenuResult;
        private string[] _inputConfirmStrings;
        private string[] _inputToolStrings;
        private string[] _inputOpt1Strings;
        private InputCodeStringWrapper _inputConfirmStrWrapper;
        private InputCodeStringWrapper _inputToolStrWrapper;
        protected InputCodeStringWrapper _inputOpt1StrWrapper;

        /// <summary>
        /// 遷移先を示すタグ
        /// </summary>
        public override void Init( object context )
        {
            base.Init( context );

            _isShowingAllDangerRange    = false;
            _isWaitingForTileMenuResult = false;
            _stageCtrl.SetActiveGridCursor( true );   // グリッド選択を有効化

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
                "SHOW\nDANGER RANGE", // 危険領域表示
                "HIDE\nDANGER RANGE", // 危険領域非表示
            };
            // OPT1アイコンの文字列を設定
            _inputOpt1Strings = new string[]
            {
                "REGISTER",   // グループ移動未登録
                "UNREGISTER", // グループ移動登録済み
            };

            _inputConfirmStrWrapper = new InputCodeStringWrapper( _inputConfirmStrings[0] );
            _inputToolStrWrapper    = new InputCodeStringWrapper( _inputToolStrings[0] );
            _inputOpt1StrWrapper    = new InputCodeStringWrapper( _inputOpt1Strings[0] );

            foreach( Player player in _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable( CHARACTER_TAG.PLAYER ) )
            {
                player.RefreshUseableSkillFlags( Combat.SituationType.NONE, 0xff );
            }
            foreach( Enemy enemy in _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable( CHARACTER_TAG.ENEMY ) )
            {
                enemy.RefreshUseableSkillFlags( Combat.SituationType.NONE, 0xff );
            }

            RefreshDispParameterView();
        }

        public override bool Update()
        {
            // グリッド選択より遷移が戻ることはないため基底の更新は行わない
            // if( base.Update() ) { return true; }

            // 全てのキャラクターが待機済みになっていれば終了
            if( _btlRtnCtrl.BtlCharaCdr.IsEndAllArmyWaitCommand( CHARACTER_TAG.PLAYER ) )
            {
                Back();

                return true;
            }

            TileDynamicData tileData = _stageCtrl.TileDataHdlr().GetTileDatas( _stageCtrl.GetCurrentGridIndex() ).Item2;
            if( tileData.CharaKey.IsValid() )
            {
                // Confirmアイコンの文字列を更新
                _inputConfirmStrWrapper.Explanation = _inputConfirmStrings[( int ) tileData.CharaKey.CharacterTag];
            }

            // TOOLアイコンの文字列を更新
            _inputToolStrWrapper.Explanation = _isShowingAllDangerRange ? _inputToolStrings[1] : _inputToolStrings[0];

            // OPT1アイコンの文字列を更新(カーソル上のキャラクターが登録済みかどうかで切り替え)
            Character cursorChara = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();
            if( null != cursorChara && cursorChara.GetCharacterTag() == CHARACTER_TAG.PLAYER )
            {
                _inputOpt1StrWrapper.Explanation = _groupMoveRegistrationList.Contains( cursorChara ) ? _inputOpt1Strings[1] : _inputOpt1Strings[0];
            }

            // グループ移動登録者のうち、行動終了等で対象外になったキャラクターを除去する
            PruneIneligibleRegistrations();

            return ( 0 <= TransitIndex );
        }

        /// <summary>
        /// 入力コードを登録します
        /// </summary>
        public override void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            _inputFcd.RegisterInputCodes(
               (GuideIcon.ALL_CURSOR, "MOVE", CanAcceptDefault, new AcceptContextInput( AcceptDirection ), GRID_DIRECTION_INPUT_INTERVAL, hashCode),
               (GuideIcon.CONFIRM, _inputConfirmStrWrapper, CanAcceptConfirm, new AcceptContextInput( AcceptConfirm ), 0.0f, hashCode),
               (GuideIcon.TOOL, _inputToolStrWrapper, CanAcceptDefault, new AcceptContextInput( AcceptTool ), 0.0f, hashCode),
               (GuideIcon.INFO, "STATUS", CanAcceptInfo, new AcceptContextInput( AcceptInfo ), 0.0f, hashCode),
               (GuideIcon.OPT1, _inputOpt1StrWrapper, CanAcceptOpt1, new AcceptContextInput( AcceptOpt1 ), 0.0f, hashCode),
               (GuideIcon.OPT2, "MENU", CanAcceptDefault, new AcceptContextInput( AcceptOpt2 ), 0.0f, hashCode)
            );
        }

        /// <summary>
        /// キャラクターコマンドへ遷移可能かを判定します
        /// </summary>
        /// <returns>コマンド選択が可能か</returns>
        protected override bool CanAcceptConfirm()
        {
            if( 0 <= TransitIndex )
            {
                return false;
            }

            Character character = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();
            if( null == character ) { return false; }

            // プレイヤーキャラクターの場合、行動終了状態でなければコマンド選択可能
            if( character.GetStatusRef.characterTag == CHARACTER_TAG.PLAYER )
            {
                // スキル予約済みの場合は、行動終了状態でも予約実行のために選択可能とする
                if( character.BattleParams.TmpParam.IsSkillQueued )
                {
                    return true;
                }

                return !character.BattleParams.TmpParam.IsEndAction();
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
        protected override bool CanAcceptInfo()
        {
            return null != _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();
        }

        /// <summary>
        /// 方向入力を受け取り、選択グリッドを操作します
        /// </summary>
        /// <param name="dir">方向入力</param>
        /// <returns>入力実行の有無</returns>
        protected override bool AcceptDirection( InputContext context )
        {
            bool isAcceptDirection = _stageCtrl.OperateGridCursorBasedOnCamera( ref context.Cursor );

            if( isAcceptDirection )
            {
                RefreshDispParameterView();
            }

            return isAcceptDirection;
        }

        /// <summary>
        /// 決定入力を受けた際の各種処理を行います
        /// </summary>
        /// <param name="isConfirm">決定入力</param>
        /// <returns>入力実行の有無</returns>
        protected override bool AcceptConfirm( InputContext context )
        {
            if( !base.AcceptConfirm( context ) ) { return false; }

            Character character = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();
            if( null == character ) { return false; }

            // プレイヤーキャラクターの場合、行動終了状態でなければコマンド選択可能
            if( character.GetStatusRef.characterTag == CHARACTER_TAG.PLAYER )
            {
                // スキル予約済みの場合は、予約に対する操作(即時実行等)の選択画面へ遷移する
                if( character.BattleParams.TmpParam.IsSkillQueued )
                {
                    TransitStateWithExit( ( int ) TransitTag.SELECT_RESERVED_ACTION );
                    return true;
                }

                TransitStateWithExit( ( int ) TransitTag.CHARACTER_COMMAND );
                // コマンドを開くことをチュートリアルへ通知
                TutorialFacade.Notify( TriggerType.OpenBattleCommand );
            }
            // 敵キャラクター、その他のキャラクターの場合、攻撃範囲表示を行う
            else
            {
                Npc npc = character as Npc;
                if( null == npc ) { return false; }

                npc.BattleLogic.ToggleDisplayDangerRange(); // 攻撃範囲表示を切り替える

                // 全ての敵及び第三勢力の攻撃範囲表示状態を確認し、全てが_isShowingAllDangerRangeの値と異なっている場合は、全危険範囲表示状態を切り替える
                bool isAllMismatch = true;
                foreach( Npc npcChara in _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable( CHARACTER_TAG.ENEMY, CHARACTER_TAG.OTHER ) )
                {
                    if( _isShowingAllDangerRange == npcChara.BattleLogic.ActionRangeCtrl.ActionableRangeRdr.IsShowingAttackableRange )
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
        /// TOOL入力を受けた際に敵・その他キャラクターの攻撃可能範囲表示を切り替えます
        /// </summary>
        /// <param name="isInput"></param>
        /// <returns></returns>
        protected override bool AcceptTool( InputContext context )
        {
            if( !base.AcceptTool( context ) ) { return false; }

            _isShowingAllDangerRange = !_isShowingAllDangerRange;

            foreach( Npc npcChara in _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable( CHARACTER_TAG.ENEMY, CHARACTER_TAG.OTHER ) )
            {
                npcChara.BattleLogic.SetDisplayDangerRange( _isShowingAllDangerRange );
            }

            return true;
        }

        /// <summary>
        /// グリッド上のキャラクターのステータス画面を開きます
        /// </summary>
        /// <param name="isInput"></param>
        /// <returns></returns>
        protected override bool AcceptInfo( InputContext context )
        {
            if( !base.AcceptInfo( context ) ) { return false; }

            // ステータス表示ステートに対象キャラクターを渡す
            SetSendTransitionContext( _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter() );

            TransitState( ( int ) TransitTag.CHARACTER_STATUS );

            return true;
        }

        /// <summary>
        /// OPT2入力を受けた際にタイルメニュー(Option/Turn End)へ遷移させます
        /// </summary>
        /// <returns>入力実行の有無</returns>
        protected override bool AcceptOpt2( InputContext context )
        {
            if( !base.AcceptOpt2( context ) ) { return false; }

            _isWaitingForTileMenuResult = true;
            TransitState( ( int ) TransitTag.SELECT_TILE_MENU );

            return true;
        }

        /// <summary>
        /// OPT1入力を受け付けるかどうかを判定します(カーソル上がプレイヤーかつ移動コマンド実行可能な場合のみ)
        /// </summary>
        protected virtual bool CanAcceptOpt1()
        {
            Character character = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();
            if( null == character || character.GetCharacterTag() != CHARACTER_TAG.PLAYER ) { return false; }

            return Command.IsExecutableMoveCommand( character, _stageCtrl );
        }

        /// <summary>
        /// カーソル上のキャラクターのグループ移動登録・解除を切り替えます
        /// </summary>
        /// <param name="character">対象キャラクター</param>
        /// <param name="wasEmpty">切り替え前にリストが空だったかどうか</param>
        protected void ToggleGroupMoveRegistration( Character character, out bool wasEmpty )
        {
            wasEmpty = _groupMoveRegistrationList.IsEmpty;

            if( _groupMoveRegistrationList.Contains( character ) )
            {
                _groupMoveRegistrationList.Remove( character );
                character.RestoreMaterialsOriginalColor();
            }
            else
            {
                _groupMoveRegistrationList.Add( character );
                character.SetMaterialsSemiTransparent();
            }
        }

        /// <summary>
        /// OPT1入力を受けた際、カーソル上のキャラクターのグループ移動登録・解除を切り替えます。
        /// この操作によって登録者が0人から1人になった場合は、グループ移動プレビューステートへ自動的に遷移します。
        /// </summary>
        protected override bool AcceptOpt1( InputContext context )
        {
            if( !base.AcceptOpt1( context ) ) { return false; }

            Character character = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();
            if( null == character ) { return false; }

            ToggleGroupMoveRegistration( character, out bool wasEmpty );

            if( wasEmpty && !_groupMoveRegistrationList.IsEmpty )
            {
                TransitState( ( int ) TransitTag.GROUP_MOVE );
            }

            return true;
        }

        /// <summary>
        /// グループ移動登録者のうち、行動終了等によって対象外になったキャラクターを登録解除します
        /// </summary>
        private void PruneIneligibleRegistrations()
        {
            if( _groupMoveRegistrationList.IsEmpty ) { return; }

            var registeredKeys = _groupMoveRegistrationList.GetAll();
            for( int i = registeredKeys.Count - 1; 0 <= i; --i )
            {
                CharacterKey key    = registeredKeys[i];
                Player character    = _btlRtnCtrl.BtlCharaCdr.GetPlayer( key );

                if( null != character && Command.IsExecutableMoveCommand( character, _stageCtrl ) ) { continue; }

                character?.RestoreMaterialsOriginalColor();
                _groupMoveRegistrationList.Remove( key );
            }
        }

        protected override void OnActivated()
        {
            base.OnActivated();

            if( _isWaitingForTileMenuResult )
            {
                _isWaitingForTileMenuResult = false;
                var menuState = GetChildren<PlSelectMenuState>( ( int ) TransitTag.SELECT_TILE_MENU );
                if( menuState != null && menuState.IsTurnEndSelected )
                {
                    TransitState( ( int ) TransitTag.TURN_END );
                }
            }
        }

        private void RefreshDispParameterView()
        {
            var gridSelectChara         = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();
            bool isActiveParamView      = ( gridSelectChara != null );
            bool isActiveLeftParamUI    = isActiveParamView && ( gridSelectChara.GetCharacterTag() == CHARACTER_TAG.PLAYER );
            bool isActiveRightParamUI   = isActiveParamView && ( gridSelectChara.GetCharacterTag() != CHARACTER_TAG.PLAYER );

            if( isActiveParamView )
            {
                ParameterWindowType windowType = ( gridSelectChara.GetCharacterTag() == CHARACTER_TAG.PLAYER )
                    ? ParameterWindowType.Left : ParameterWindowType.Right;

                var layerMaskIndex = BattleRoutinePresenter.GetLayerMaskIndexFromWinType( windowType );
                _presenter.CharaParamView( windowType ).AssignCharacter( gridSelectChara, layerMaskIndex );
            }

            _presenter.CharaParamView( ParameterWindowType.Left ).SetActive( isActiveLeftParamUI );
            _presenter.CharaParamView( ParameterWindowType.Right ).SetActive( isActiveRightParamUI );
        }
    }
}