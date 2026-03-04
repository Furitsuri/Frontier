using Frontier.Combat;
using Frontier.Combat.Skill;
using Frontier.Entities;
using Frontier.Stage;
using Frontier.StateMachine;
using UnityEngine;
using static Constants;
using static Frontier.UI.BattleUISystem;

namespace Frontier.Battle
{
    public class EmAttackState : UnitPhaseState
    {
        private enum EmAttackPhase
        {
            EM_ATTACK_CONFIRM = 0,
            EM_ATTACK_EXECUTE,
            EM_ATTACK_END,
        }

        private EmAttackPhase _phase;
        private string[] _playerSkillNames = null;
        private Enemy _attackCharacter = null;
        private Character _targetCharacter = null;
        private CharacterAttackSequence _attackSequence = null;

        public override void Init()
        {
            base.Init();

            _attackSequence = _hierarchyBld.InstantiateWithDiContainer<CharacterAttackSequence>( false );
            _attackCharacter = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter() as Enemy;
            Debug.Assert( _attackCharacter != null );

            // 現在選択中のキャラクター情報を取得して攻撃範囲を表示
            _attackCharacter.BattleLogic.ActionRangeCtrl.SetupAttackableRangeData( _attackCharacter.BattleParams.TmpParam.CurrentTileIndex );
            _attackCharacter.BattleLogic.ActionRangeCtrl.DrawAttackableRange();

            // 攻撃可能なタイル内に攻撃可能対象がいた場合にグリッドを合わせる
            if( _stageCtrl.TileDataHdlr().CorrectAttackableTileIndexs( _attackCharacter.BattleLogic.ActionRangeCtrl, _attackCharacter.BattleLogic.GetAi().GetTargetCharacter() ) )
            {
                _stageCtrl.BindToGridCursor( GridCursorState.ATTACK, _attackCharacter );    // アタッカーキャラクターの設定
                _uiSystem.BattleUi.SetAttackCursorE2PActive( true );                           // アタックカーソルUI表示
            }

            _targetCharacter = _attackCharacter.BattleLogic.GetAi().GetTargetCharacter();
            _stageCtrl.ApplyCurrentGrid2CharacterTile( _attackCharacter );

            _playerSkillNames = _targetCharacter.GetStatusRef.GetEquipSkillNames();

            // 攻撃者の向きを設定
            var targetTileData = _stageCtrl.GetTileStaticData( _targetCharacter.BattleParams.TmpParam.CurrentTileIndex );
            _attackCharacter.GetTransformHandler.RotateToPosition( targetTileData.CharaStandPos );
            var attackerTileData = _stageCtrl.GetTileStaticData( _attackCharacter.BattleParams.TmpParam.CurrentTileIndex );
            _targetCharacter.GetTransformHandler.RotateToPosition( attackerTileData.CharaStandPos );

            // 攻撃シーケンスを初期化
            _attackSequence.Init();

            // パラメータビューにキャラクターを割り当て
            _presenter.AssignCharacterToParameterView( _targetCharacter, ParameterWindowType.Left );
            _presenter.AssignCharacterToParameterView( _attackCharacter, ParameterWindowType.Right );
            _presenter.SetActiveParamView( true, ParameterWindowType.Left );
            _presenter.SetActiveParamView( true, ParameterWindowType.Right );

            _phase = EmAttackPhase.EM_ATTACK_CONFIRM;
        }

        public override bool Update()
        {
            _presenter.UpdateLeftParameterView();
            _presenter.UpdateRightParameterView();

            // 攻撃可能状態でなければ何もしない
            if( _stageCtrl.GetGridCursorControllerState() != GridCursorState.ATTACK )
            {
                return false;
            }

            switch( _phase )
            {
                case EmAttackPhase.EM_ATTACK_CONFIRM:
                    // 使用スキルを選択する
                    _attackCharacter.BattleLogic.SelectUseSkills( SituationType.ATTACK );
                    _targetCharacter.BattleLogic.SelectUseSkills( SituationType.DEFENCE );

                    // 予測ダメージを適応する
                    _btlRtnCtrl.BtlCharaCdr.ApplyDamageExpect( _attackCharacter, _targetCharacter );

                    // ダメージ予測表示UIを表示
                    _uiSystem.BattleUi.ToggleBattleExpect( true );

                    break;
                case EmAttackPhase.EM_ATTACK_EXECUTE:
                    if( _attackSequence.Update() )
                    {
                        _phase = EmAttackPhase.EM_ATTACK_END;
                    }
                    break;
                case EmAttackPhase.EM_ATTACK_END:
                    // 攻撃したキャラクターの攻撃コマンドを選択不可にする
                    _attackCharacter.BattleParams.TmpParam.SetEndCommandStatus( COMMAND_TAG.ATTACK, true );
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
                // 破棄
                diedCharacter.Dispose();
            }

            // アタッカーキャラクターの設定を解除
            _stageCtrl.ClearGridCursroBind();
            // 予測ダメージをリセット
            _attackCharacter.BattleParams.TmpParam.SetExpectedHpChange( 0, 0 );
            _targetCharacter.BattleParams.TmpParam.SetExpectedHpChange( 0, 0 );

            // アタックカーソルUI非表示
            _uiSystem.BattleUi.SetAttackCursorP2EActive( false );
            // ダメージ予測表示UIを非表示
            _uiSystem.BattleUi.ToggleBattleExpect( false );
            // 使用スキルの点滅を非表示
            for( int i = 0; i < EQUIPABLE_SKILL_MAX_NUM; ++i )
            {
                bool targetUseable = _targetCharacter.BattleLogic.CanToggleEquipSkill( i, SituationType.DEFENCE );
                bool attackUseable = _attackCharacter.BattleLogic.CanToggleEquipSkill( i, SituationType.ATTACK );
                _uiSystem.BattleUi.GetPlayerParamSkillBox( i ).SetFlickEnabled( false );
                _presenter.SetUseableSkillOnParamView( i, targetUseable, ParameterWindowType.Left );
                _uiSystem.BattleUi.GetEnemyParamSkillBox( i ).SetFlickEnabled( false );
                _presenter.SetUseableSkillOnParamView( i, attackUseable, ParameterWindowType.Right );
            }
            // 使用スキルコスト見積もりをリセット
            _attackCharacter.GetStatusRef.ResetConsumptionActionGauge();
            _attackCharacter.BattleParams.SkillModifiedParam.Reset();
            _targetCharacter.GetStatusRef.ResetConsumptionActionGauge();
            _targetCharacter.BattleParams.SkillModifiedParam.Reset();
            _btlRtnCtrl.BtlCharaCdr.ClearAllTileMeshes(); // タイルメッシュの描画をすべてクリア
            // 選択グリッドを表示
            // ※この攻撃の直後にプレイヤーフェーズに移行した場合、一瞬の間、選択グリッドが表示され、
            //   その後プレイヤーに選択グリッドが移るという状況になります。
            //   その挙動が少しバグのように見えてしまうので、消去したままにすることにし、
            //   次のキャラクターが行動開始する際に表示するようにします。
            // Stage.StageController.Instance.SetGridCursorControllerActive(true);

            base.ExitState();
        }

        public override void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            _inputFcd.RegisterInputCodes(
               (GuideIcon.CONFIRM, "Confirm", CanAcceptConfirm, new AcceptContextInput( AcceptConfirm ), 0.0f, hashCode),
               (GuideIcon.SUB1, _playerSkillNames[0], CanAcceptSub1, new AcceptContextInput( AcceptSub1 ), 0.0f, hashCode),
               (GuideIcon.SUB2, _playerSkillNames[1], CanAcceptSub2, new AcceptContextInput( AcceptSub2 ), 0.0f, hashCode),
               (GuideIcon.SUB3, _playerSkillNames[2], CanAcceptSub3, new AcceptContextInput( AcceptSub3 ), 0.0f, hashCode),
               (GuideIcon.SUB4, _playerSkillNames[3], CanAcceptSub4, new AcceptContextInput( AcceptSub4 ), 0.0f, hashCode)
            );
        }

        /// <summary>
        /// 決定入力受付の可否を判定します
        /// </summary>
        /// <returns>決定入力受付の可否</returns>
        protected override bool CanAcceptConfirm()
        {
            if( !CanAcceptDefault() ) return false;

            if( EmAttackPhase.EM_ATTACK_CONFIRM == _phase ) return true;

            return false;
        }

        /// <summary>
        /// サブ1の入力の受付可否を判定します
        /// </summary>
        /// <returns>サブ1の入力の受付可否</returns>
        protected override bool CanAcceptSub1()
        {
            if( !CanAcceptDefault() ) return false;

            if( EmAttackPhase.EM_ATTACK_CONFIRM != _phase ) return false;

            if( _playerSkillNames[0].Length <= 0 ) return false;

            if( _targetCharacter is not Player ) return false;

            bool useable = _targetCharacter.BattleLogic.CanToggleEquipSkill( 0, SituationType.DEFENCE );
            _presenter.SetUseableSkillOnParamView( 0, useable, ParameterWindowType.Left );

            return useable;
        }

        protected override bool CanAcceptSub2()
        {
            if( !CanAcceptDefault() ) return false;

            if( EmAttackPhase.EM_ATTACK_CONFIRM != _phase ) return false;

            if( _playerSkillNames[1].Length <= 0 ) return false;

            if( _targetCharacter is not Player ) return false;

            bool useable = _targetCharacter.BattleLogic.CanToggleEquipSkill( 1, SituationType.DEFENCE );
            _presenter.SetUseableSkillOnParamView( 1, useable, ParameterWindowType.Left );

            return useable;
        }

        protected override bool CanAcceptSub3()
        {
            if( !CanAcceptDefault() ) return false;

            if( EmAttackPhase.EM_ATTACK_CONFIRM != _phase ) return false;

            if( _playerSkillNames[2].Length <= 0 ) return false;

            if( _targetCharacter is not Player ) return false;

            bool useable = _targetCharacter.BattleLogic.CanToggleEquipSkill( 2, SituationType.DEFENCE );
            _presenter.SetUseableSkillOnParamView( 2, useable, ParameterWindowType.Left );

            return useable;
        }

        protected override bool CanAcceptSub4()
        {
            if( !CanAcceptDefault() ) return false;

            if( EmAttackPhase.EM_ATTACK_CONFIRM != _phase ) return false;

            if( _playerSkillNames[3].Length <= 0 ) return false;

            if( _targetCharacter is not Player ) return false;

            bool useable = _targetCharacter.BattleLogic.CanToggleEquipSkill( 3, SituationType.DEFENCE );
            _presenter.SetUseableSkillOnParamView( 3, useable, ParameterWindowType.Left );

            return useable;
        }

        /// <summary>
        /// 決定入力を受け取った際の処理を行います
        /// </summary>
        /// <param name="isConfirm">決定入力の有無</param>
        protected override bool AcceptConfirm( InputContext context )
        {
            if( !base.AcceptConfirm( context ) ) { return false; }

            // キャラクターのアクションゲージを消費
            _attackCharacter.BattleLogic.ConsumeActionGauge();
            _targetCharacter.BattleLogic.ConsumeActionGauge();

            _stageCtrl.SetGridCursorControllerActive( false );                      // 選択グリッドを一時非表示
            _uiSystem.BattleUi.SetAttackCursorE2PActive( false );                   // アタックカーソルUI非表示
            _uiSystem.BattleUi.ToggleBattleExpect( false );                         // ダメージ予測表示UIを非表示
            _btlRtnCtrl.BtlCharaCdr.ClearAllTileMeshes();                           // タイルメッシュの描画をすべてクリア
            _attackSequence.StartSequence( _attackCharacter, _targetCharacter );    // 攻撃シーケンスの開始
            UnregisterInputCodes( Hash.GetStableHash( GetType().Name ) );           // 現在の入力コードを登録解除

            _phase = EmAttackPhase.EM_ATTACK_EXECUTE;

            return true;
        }

        protected override bool AcceptSub1( InputContext context )
        {
			if( !base.AcceptSub1( context ) ) { return false; }

            _targetCharacter.BattleLogic.ToggleUseSkill( 0 );
            _presenter.SetSkillFlickOnParamView( 0, _targetCharacter.BattleLogic.IsUsingEquipSkill( 0 ), ParameterWindowType.Left );

            return true;

        }

        protected override bool AcceptSub2( InputContext context )
        {
			if( !base.AcceptSub2( context ) ) { return false; }

            _targetCharacter.BattleLogic.ToggleUseSkill( 1 );
            _presenter.SetSkillFlickOnParamView( 1, _targetCharacter.BattleLogic.IsUsingEquipSkill( 1 ), ParameterWindowType.Left );

            return true;
        }

        protected override bool AcceptSub3( InputContext context )
        {
			if( !base.AcceptSub3( context ) ) { return false; }

            _targetCharacter.BattleLogic.ToggleUseSkill( 2 );
            _presenter.SetSkillFlickOnParamView( 2, _targetCharacter.BattleLogic.IsUsingEquipSkill( 2 ), ParameterWindowType.Left );

            return true;
        }

        protected override bool AcceptSub4( InputContext context )
        {
			if( !base.AcceptSub4( context ) ) { return false; }

            _targetCharacter.BattleLogic.ToggleUseSkill( 3 );
            _presenter.SetSkillFlickOnParamView( 3, _targetCharacter.BattleLogic.IsUsingEquipSkill( 3 ), ParameterWindowType.Left );

            return true;
        }
    }
}