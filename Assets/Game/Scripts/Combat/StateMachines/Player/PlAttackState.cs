using Frontier.Combat;
using Frontier.Entities;
using Frontier.Sequences;
using Frontier.Stage;
using Frontier.UI;
using System;
using Zenject;
using static Constants;

namespace Frontier.Battle
{
    public class PlAttackState : PlPhaseStateBase
    {
        protected enum PlAttackPhase
        {
            PL_ATTACK_SELECT_GRID = 0,
            PL_ATTACK_EXECUTE,
            PL_ATTACK_END,
        }

        private enum TransitTag
        {
            CHARACTER_STATUS = 0,
        }

        [Inject] private SequenceFacade _sequenceFcd = null;

        protected PlAttackPhase _phase = PlAttackPhase.PL_ATTACK_SELECT_GRID;
        protected int _curentGridIndex = -1;
        protected string[] _playerSkillNames = null;
        protected Character _targetCharacter = null;
        protected CharacterAttackSequence _attackSequence = null;
        protected Func<InputContext, bool>[] AccespuSubs;

        protected void PlPhaseStateInit()
        {
            base.Init( null );
        }

        /// <summary>
        /// 初期化します
        /// </summary>
        public override void Init( object context )
        {
            base.Init( context);

            _playerSkillNames   = _plOwner.GetStatusRef.GetEquipSkillNames();
            _phase              = PlAttackPhase.PL_ATTACK_SELECT_GRID;
            _curentGridIndex    = _stageCtrl.GetCurrentGridIndex();
            _targetCharacter    = null;
            AccespuSubs         = new Func<InputContext, bool>[]
            {
                ( context ) => base.AcceptSub1( context ),
                ( context ) => base.AcceptSub2( context ),
                ( context ) => base.AcceptSub3( context ),
                ( context ) => base.AcceptSub4( context )
            };

            // 現在選択中のキャラクター情報を取得して攻撃範囲を表示
            int dprtTileIndex = _plOwner.BattleParams.TmpParam.CurrentTileIndex;
            _plOwner.BattleLogic.ActionRangeCtrl.SetupAttackableRangeData( dprtTileIndex );
            _plOwner.BattleLogic.ActionRangeCtrl.DrawAttackableRange();

            // 使用可能スキルの更新
            _plOwner.RefreshUseableSkillFlags( Combat.SituationType.ATTACK, Methods.ToBit( ActionType.BUFF ) );

            // 攻撃可能なグリッド内に敵がいた場合に標的グリッドを合わせる
            if( _stageCtrl.TileDataHdlr().CorrectAttackableTileIndexs( _plOwner.BattleLogic.ActionRangeCtrl, _btlRtnCtrl.BtlCharaCdr.GetNearestLineOfSightCharacter( _plOwner, CHARACTER_TAG.ENEMY ) ) )
            {
                _stageCtrl.BindToGridCursor( GridCursorState.ATTACK, _plOwner );    // アタッカーキャラクターの設定
                _presenter.SetActiveActionResultExpect( true, ParameterWindowType.Left );    // アクション対象指定関連のUIを表示
            }
        }

        public override bool Update()
        {
            bool isActiveRightParameterView = ( null != _targetCharacter );
            _presenter.CharaParamView( ParameterWindowType.Right ).SetActive( isActiveRightParameterView );

            if( base.Update() )
            {
                return true;
            }

            // 攻撃可能状態でなければ何もしない
            if( _stageCtrl.GetGridCursorControllerState() != GridCursorState.ATTACK )
            {
                return false;
            }

            switch( _phase )
            {
                case PlAttackPhase.PL_ATTACK_SELECT_GRID:
                    // グリッド上のキャラクターを取得
                    var prevTargetCharacter = _targetCharacter;
                    _targetCharacter = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();

                    // 選択キャラクターが更新された場合はパラメータUIへの描画対象と、キャラクターの向きを更新
                    if( prevTargetCharacter != _targetCharacter )
                    {
                        var layerMaskIndex = BattleRoutinePresenter.GetLayerMaskIndexFromWinType( ParameterWindowType.Right );
                        _presenter.CharaParamView( ParameterWindowType.Right ).AssignCharacter( _targetCharacter, layerMaskIndex );

                        if( null != prevTargetCharacter )
                        {
                            prevTargetCharacter.GetTransformHandler.ResetRotationOrder();
                        }

                        var targetTileData = _stageCtrl.GetTileStaticData( _targetCharacter.BattleParams.TmpParam.CurrentTileIndex );
                        _plOwner.GetTransformHandler.RotateToPosition( targetTileData.CharaStandPos );
                        var attackerTileData = _stageCtrl.GetTileStaticData( _plOwner.BattleParams.TmpParam.CurrentTileIndex );
                        _targetCharacter.GetTransformHandler.RotateToPosition( attackerTileData.CharaStandPos );
                    }

                    // 使用スキルを選択する
                    _plOwner.BattleLogic.SelectUseSkills( SituationType.ATTACK );
                    _targetCharacter.RefreshUseableSkillFlags( SituationType.DEFENCE, Methods.ToBit( ActionType.BUFF ) | Methods.ToBit( ActionType.SPECIAL ) );
                    _targetCharacter.BattleLogic.SelectUseSkills( SituationType.DEFENCE );

                    // 予測ダメージを適応する
                    _btlRtnCtrl.BtlCharaCdr.ApplyDamageExpect( _plOwner, _targetCharacter );
                        
                    break;
                case PlAttackPhase.PL_ATTACK_EXECUTE:
                    if( _attackSequence.Update() )
                    {
                        _phase = PlAttackPhase.PL_ATTACK_END;
                    }

                    break;
                case PlAttackPhase.PL_ATTACK_END:
                    // 攻撃したキャラクターの攻撃コマンドを選択不可にする
                    _plOwner.BattleParams.TmpParam.SetEndCommandStatus( COMMAND_TAG.ATTACK, true );
                    // 攻撃コマンド以外も選択不可にする（攻撃後は移動やスキルも使用できないようにするため）
                    _plOwner.ClearCommandHistory();
                    // コマンド選択に戻る
                    Back();

                    return true;
            }

            return false;
        }

        public override object ExitState()
        {
            //死亡判定を通知(相手のカウンターによって倒される可能性もあるため、攻撃者と被攻撃者の両方を判定)
            Character diedCharacter = null;// _attackSequence.GetDiedCharacter();
            if( diedCharacter != null )
            {
                var key = new CharacterKey( diedCharacter.GetStatusRef.characterTag, diedCharacter.GetStatusRef.characterIndex );
                NorifyCharacterDied( key );
                diedCharacter.Dispose();    // 破棄
            }

            _stageCtrl.ClearGridCursorBind();                                                       // アタッカーキャラクターの設定を解除
            _presenter.SetActiveActionResultExpect( false, ParameterWindowType.Left );    // アクション対象指定関連のUIを非表示

            // 予測ダメージと使用スキルコスト見積もりをリセット
            if( null != _plOwner )
            {
                _plOwner.BattleParams.TmpParam.SetExpectedHpChange( 0, 0 );
            }
            if( null != _targetCharacter )
            {
                _targetCharacter.BattleParams.TmpParam.SetExpectedHpChange( 0, 0 );
            }

            _btlRtnCtrl.BtlCharaCdr.ClearAllTileMeshes();       // タイルメッシュの描画をすべてクリア
            _stageCtrl.SetGridCursorControllerActive( true );   // 選択グリッドを表示

            return base.ExitState();
        }

        /// <summary>
        /// 入力コードを登録します
        /// </summary>
        public override void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            // 入力ガイドを登録
            _inputFcd.RegisterInputCodes(
               (GuideIcon.ALL_CURSOR,   "TARGET SELECT", CanAcceptDirection, new AcceptContextInput( AcceptDirection ), MENU_DIRECTION_INPUT_INTERVAL, hashCode),
               (GuideIcon.CONFIRM,      "CONFIRM", CanAcceptConfirm, new AcceptContextInput( AcceptConfirm ), 0.0f, hashCode),
               (GuideIcon.CANCEL,       "BACK", CanAcceptCancel, new AcceptContextInput( AcceptCancel ), 0.0f, hashCode),
               (GuideIcon.INFO,         "INFO", CanAcceptInfo, new AcceptContextInput( AcceptInfo ), 0.0f, hashCode),
               (GuideIcon.SUB1,         _playerSkillNames[0], CanAcceptSub1, new AcceptContextInput( AcceptSub1 ), 0.0f, hashCode),
               (GuideIcon.SUB2,         _playerSkillNames[1], CanAcceptSub2, new AcceptContextInput( AcceptSub2 ), 0.0f, hashCode),
               (GuideIcon.SUB3,         _playerSkillNames[2], CanAcceptSub3, new AcceptContextInput( AcceptSub3 ), 0.0f, hashCode),
               (GuideIcon.SUB4,         _playerSkillNames[3], CanAcceptSub4, new AcceptContextInput( AcceptSub4 ), 0.0f, hashCode)
            );
        }

        /// <summary>
        /// 操作対象のプレイヤーを設定します
        /// </summary>
        protected override void AdaptSelectPlayer()
        {
            // グリッドカーソルで選択中のプレイヤーを取得
            _plOwner = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter() as Player;
            NullCheck.AssertNotNull( _plOwner, nameof( _plOwner ) );
        }

        protected override void OnActivated()
        {
            base.OnActivated();

            // パラメータビューにキャラクターを割り当て
            var layerMaskIndex = BattleRoutinePresenter.GetLayerMaskIndexFromWinType( ParameterWindowType.Left );
            _presenter.CharaParamView( ParameterWindowType.Left ).AssignCharacter( _plOwner, layerMaskIndex );
        }

        /// <summary>
        /// 方向入力の受付可否を判定します
        /// </summary>
        /// <returns>方向入力の受付可否</returns>
        protected override bool CanAcceptDirection()
        {
            if( !CanAcceptDefault() ) { return false; }
            if( PlAttackPhase.PL_ATTACK_SELECT_GRID != _phase ) { return false; }   // 攻撃対象選択フェーズでない場合は不可
            if( _stageCtrl.GetAttackabkeTargetNum() <= 1 ) { return false; }   // 攻撃可能な標的数が1より大きくなければ不可

            return true;
        }

        /// <summary>
        /// 決定入力の受付可否を判定します
        /// </summary>
        /// <returns>決定入力の受付可否</returns>
        protected override bool CanAcceptConfirm()
        {
            if( !CanAcceptDefault() ) { return false; }
            if( PlAttackPhase.PL_ATTACK_SELECT_GRID != _phase ) { return false; }   // 攻撃対象選択フェーズでない場合は不可

            return true;
        }

        /// <summary>
        /// キャンセル入力の受付可否を判定します
        /// </summary>
        /// <returns>キャンセル入力の受付可否</returns>
        protected override bool CanAcceptCancel()
        {
            // Confirmと同一
            return CanAcceptConfirm();
        }

        /// <summary>
        /// PL_ATTACK_SELECT_GRID時のみ、相手のステータス情報を表示可能とします
        /// </summary>
        /// <returns></returns>
        protected override bool CanAcceptInfo()
        {
            if( !CanAcceptDefault() ) { return false; }
            if( PlAttackPhase.PL_ATTACK_SELECT_GRID != _phase ) { return false; }   // 攻撃対象選択フェーズでない場合は終了
            return true;
        }

        protected override bool CanAcceptSub1() => CanAcceptSub( 0 );
        protected override bool CanAcceptSub2() => CanAcceptSub( 1 );
        protected override bool CanAcceptSub3() => CanAcceptSub( 2 );
        protected override bool CanAcceptSub4() => CanAcceptSub( 3 );

        /// <summary>
        /// 方向入力を受け取った際の処理を行います
        /// </summary>
        /// <param name="dir">方向入力</param>
        /// <returns>入力実行の有無</returns>
        protected override bool AcceptDirection( InputContext context )
        {
            if( _stageCtrl.OperateTargetSelect( context.Cursor ) )
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 決定入力を受け取った際の処理を行います
        /// </summary>
        /// <param name="isInput">決定入力</param>
        /// <returns>入力実行の有無</returns>
        protected override bool AcceptConfirm( InputContext context )
        {
			if( !base.AcceptConfirm( context ) ) { return false; }

			// 選択したキャラクターが敵である場合は攻撃開始
			if( _targetCharacter != null && _targetCharacter.GetStatusRef.characterTag == CHARACTER_TAG.ENEMY )
            {
                // キャラクターのアクションゲージを消費
                _plOwner.BattleLogic.ConsumeActionGauge();
                _targetCharacter.BattleLogic.ConsumeActionGauge();

                _stageCtrl.SetGridCursorControllerActive( false );                                      // 選択グリッドを一時非表示
                _presenter.SetActiveActionResultExpect( false, ParameterWindowType.Left );    // アクション対象指定関連のUIを非表示
                _btlRtnCtrl.BtlCharaCdr.ClearAllTileMeshes();                                           // タイルメッシュの描画をすべてクリア
                UnregisterInputCodes( Hash.GetStableHash( GetType().Name ) );                           // 現在の入力コードを登録解除

                // 自己バフスキルの登録(バフスキルが使用されていれば使用可能スキルを更新)
                if( _plOwner.BattleLogic.RegistSelfBuffSequences() )
                {
                    _plOwner.RefreshUseableSkillFlags( SituationType.ATTACK, Methods.ToBit( ActionType.BUFF ) );
                }

                _sequenceFcd.RegistAttack( _plOwner, _targetCharacter );          // 攻撃シーケンスの開始

                _phase = PlAttackPhase.PL_ATTACK_END;

                return true;
            }

            return false;
        }

        protected override bool AcceptCancel( InputContext context )
        {
            if( !base.AcceptCancel( context ) ) { return false; }

            // 攻撃対象キャラクターの向きをリセット
            if( null != _targetCharacter )
            {
                _targetCharacter.GetTransformHandler.ResetRotationOrder();
            }

            _plOwner.BattleLogic.RevertSkillsToggledOn();

            return true;
        }

        protected override bool AcceptInfo( InputContext context )
        {
            if( !base.AcceptInfo( context ) ) { return false; }

            TransitState( ( int ) TransitTag.CHARACTER_STATUS );

            return false;
        }

        protected override bool AcceptSub1( InputContext context ) => AcceptSub( 0, context );
        protected override bool AcceptSub2( InputContext context ) => AcceptSub( 1, context );
        protected override bool AcceptSub3( InputContext context ) => AcceptSub( 2, context );
        protected override bool AcceptSub4( InputContext context ) => AcceptSub( 3, context );

        private bool CanAcceptSub( int index )
        {
            if( !CanAcceptConfirm() )                   { return false; }
            if( _playerSkillNames[index].Length <= 0 )  { return false; }

            return _plOwner.BattleParams.TmpParam.IsUseableSkill[index];
        }

        private bool AcceptSub( int index, InputContext context )
        {
            if( !AccespuSubs[index]( context ) ) { return false; }

            _plOwner.BattleLogic.ToggleEquipSkill( index );
            // 使用可能スキルの更新
            _plOwner.RefreshUseableSkillFlags( SituationType.ATTACK, Methods.ToBit( ActionType.BUFF ) );

            return true;
        }
    }
}