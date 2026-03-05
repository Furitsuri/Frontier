using Frontier.Combat;
using Frontier.Combat.Skill;
using Frontier.Entities;
using Frontier.Stage;
using Frontier.UI;
using System;
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

        protected PlAttackPhase _phase = PlAttackPhase.PL_ATTACK_SELECT_GRID;
        protected int _curentGridIndex = -1;
        protected string[] _playerSkillNames = null;
        protected Character _targetCharacter = null;
        protected CharacterAttackSequence _attackSequence = null;
        private Func<InputContext, bool>[] AccespuSubs;

        protected void PlPhaseStateInit()
        {
            base.Init();
        }

        /// <summary>
        /// 初期化します
        /// </summary>
        public override void Init()
        {
            base.Init();

            _playerSkillNames   = _plOwner.GetStatusRef.GetEquipSkillNames();
            _attackSequence     = _hierarchyBld.InstantiateWithDiContainer<CharacterAttackSequence>( false );
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

            // 攻撃可能なグリッド内に敵がいた場合に標的グリッドを合わせる
            if( _stageCtrl.TileDataHdlr().CorrectAttackableTileIndexs( _plOwner.BattleLogic.ActionRangeCtrl, _btlRtnCtrl.BtlCharaCdr.GetNearestLineOfSightCharacter( _plOwner, CHARACTER_TAG.ENEMY ) ) )
            {
                _stageCtrl.BindToGridCursor( GridCursorState.ATTACK, _plOwner );    // アタッカーキャラクターの設定
                _uiSystem.BattleUi.SetAttackCursorP2EActive( true );                // アタックカーソルUI表示
            }

            // 攻撃シーケンスを初期化
            _attackSequence.Init();

            _presenter.AssignCharacterToParameterView( _plOwner, UI.ParameterWindowType.Left );
        }

        public override bool Update()
        {
            _presenter.UpdateParameterView( ParameterWindowType.Left );
            bool isActiveRightParameterView = ( null != _targetCharacter );
            _presenter.SetActiveParamView( isActiveRightParameterView, UI.ParameterWindowType.Right );
            if( isActiveRightParameterView ) { _presenter.UpdateParameterView( ParameterWindowType.Right ); }

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
                        _presenter.AssignCharacterToParameterView( _targetCharacter, UI.ParameterWindowType.Right );

                        if( null != prevTargetCharacter )
                        {
                            prevTargetCharacter.GetTransformHandler.ResetRotationOrder();
                        }

                        var targetTileData = _stageCtrl.GetTileStaticData( _targetCharacter.BattleParams.TmpParam.CurrentTileIndex );
                        _plOwner.GetTransformHandler.RotateToPosition( targetTileData.CharaStandPos );
                        var attackerTileData = _stageCtrl.GetTileStaticData( _plOwner.BattleParams.TmpParam.CurrentTileIndex );
                        _targetCharacter.GetTransformHandler.RotateToPosition( attackerTileData.CharaStandPos );
                    }

                    // ダメージ予測表示UIを表示
                    _uiSystem.BattleUi.ToggleBattleExpect( true );

                    // 使用スキルを選択する
                    _plOwner.BattleLogic.SelectUseSkills( SituationType.ATTACK );
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

        public override void ExitState()
        {
            //死亡判定を通知(相手のカウンターによって倒される可能性もあるため、攻撃者と被攻撃者の両方を判定)
            Character diedCharacter = _attackSequence.GetDiedCharacter();
            if( diedCharacter != null )
            {
                var key = new CharacterKey( diedCharacter.GetStatusRef.characterTag, diedCharacter.GetStatusRef.characterIndex );
                NorifyCharacterDied( key );
                diedCharacter.Dispose();    // 破棄
            }

            // アタッカーキャラクターの設定を解除
            _stageCtrl.ClearGridCursroBind();

            // アタックカーソルUI非表示
            _uiSystem.BattleUi.SetAttackCursorP2EActive( false );

            // ダメージ予測表示UIを非表示
            _uiSystem.BattleUi.ToggleBattleExpect( false );

            // 使用スキルの点滅を非表示
            for( int i = 0; i < EQUIPABLE_SKILL_MAX_NUM; ++i )
            {
                _uiSystem.BattleUi.GetPlayerParamSkillBox( i ).SetFlickEnabled( false );
                _presenter.SetUseableSkillOnParamView( i, true, ParameterWindowType.Left );
                _uiSystem.BattleUi.GetEnemyParamSkillBox( i ).SetFlickEnabled( false );
                _presenter.SetUseableSkillOnParamView( i, true, ParameterWindowType.Right );
            }

            // 予測ダメージと使用スキルコスト見積もりをリセット
            if( null != _plOwner )
            {
                _plOwner.BattleParams.TmpParam.SetExpectedHpChange( 0, 0 );
                _plOwner.GetStatusRef.ResetConsumptionActionGauge();
                _plOwner.BattleParams.SkillModifiedParam.Reset();
            }
            if( null != _targetCharacter )
            {
                _targetCharacter.BattleParams.TmpParam.SetExpectedHpChange( 0, 0 );
                _targetCharacter.GetStatusRef.ResetConsumptionActionGauge();
                _targetCharacter.BattleParams.SkillModifiedParam.Reset();
            }

            _btlRtnCtrl.BtlCharaCdr.ClearAllTileMeshes();       // タイルメッシュの描画をすべてクリア
            _stageCtrl.SetGridCursorControllerActive( true );   // 選択グリッドを表示

            base.ExitState();
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

                _stageCtrl.SetGridCursorControllerActive( false );              // 選択グリッドを一時非表示
                _uiSystem.BattleUi.SetAttackCursorP2EActive( false );           // アタックカーソルUI非表示
                _uiSystem.BattleUi.ToggleBattleExpect( false );                 // ダメージ予測表示UIを非表示
                _btlRtnCtrl.BtlCharaCdr.ClearAllTileMeshes();                   // タイルメッシュの描画をすべてクリア
                _attackSequence.StartSequence( _plOwner, _targetCharacter );    // 攻撃シーケンスの開始
                UnregisterInputCodes( Hash.GetStableHash( GetType().Name ) );   // 現在の入力コードを登録解除

                _phase = PlAttackPhase.PL_ATTACK_EXECUTE;

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

            _plOwner.BattleLogic.ResetUseSkills();
            _targetCharacter.BattleLogic.ResetUseSkills();

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
            if( !CanAcceptConfirm() ) return false;
            if( _playerSkillNames[index].Length <= 0 ) return false;

            bool useable = _plOwner.BattleLogic.CanToggleEquipSkill(
                index,
                SituationType.ATTACK,
                Methods.ToBit( SkillType.BUFF )
            );

            _presenter.SetUseableSkillOnParamView( index, useable, ParameterWindowType.Left );

            return useable;
        }

        private bool AcceptSub( int index, InputContext context )
        {
            if( !AccespuSubs[index]( context ) ) { return false; }

            _plOwner.BattleLogic.ToggleUseSkill( index );
            _presenter.SetSkillFlickOnParamView( index, _plOwner.BattleLogic.IsUsingEquipSkill( index ), ParameterWindowType.Left );

            return true;
        }
    }
}