using Frontier.Combat;
using Frontier.Entities;
using Frontier.Sequences;
using Frontier.Stage;
using Frontier.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using static Constants;

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

        [Inject] private SequenceFacade _sequenceFcd = null;

        private EmAttackPhase _phase;
        private string[] _targetCharaSkillNames = null;
        private Enemy _attackCharacter = null;
        private Character _targetCharacter = null;
        private Func<InputContext, bool>[] AccespuSubs;

        public override void Init( object context )
        {
            base.Init( context);

            _attackCharacter = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter() as Enemy;
            Debug.Assert( _attackCharacter != null );
            AccespuSubs = new Func<InputContext, bool>[]
            {
                ( context ) => base.AcceptSub1( context ),
                ( context ) => base.AcceptSub2( context ),
                ( context ) => base.AcceptSub3( context ),
                ( context ) => base.AcceptSub4( context )
            };

            // 現在選択中のキャラクター情報を取得して攻撃範囲を表示
            _attackCharacter.BattleLogic.ActionRangeCtrl.SetupAttackableRangeData( _attackCharacter.BattleParams.TmpParam.CurrentTileIndex );
            _attackCharacter.BattleLogic.ActionRangeCtrl.DrawAttackableRange();

			// アタッカーキャラクターの設定
			_stageCtrl.BindGridCursor( GridCursorState.ATTACK, _attackCharacter );

            List<int> tileIndicies = new List<int>();

            foreach( var tileDynamicData in _attackCharacter.BattleLogic.ActionRangeCtrl.ActionableTileData.AttackableTileMap )
            {
                if( Methods.HasAllFlags( tileDynamicData.Value.Flag, TileBitFlag.ATTACKABLE_TARGET_EXIST ) )
                {
                    tileIndicies.Add( tileDynamicData.Key );
                }
            }

            if( 0 < tileIndicies.Count )
            {
                _stageCtrl.MoveGridCursorToAttackableTile( _attackCharacter.BattleLogic.GetAi().GetTargetCharacter() );
                _presenter.SetActiveActionResultExpect( true, ParameterWindowType.Right );
            }

            _targetCharacter = _attackCharacter.BattleLogic.GetAi().GetTargetCharacter();
            _stageCtrl.ApplyCurrentGrid2CharacterTile( _attackCharacter );

            _targetCharaSkillNames = _targetCharacter.GetStatusRef.GetEquipSkillNames();

            // 攻撃者の向きを設定
            var targetTileData = _stageCtrl.GetTileStaticData( _targetCharacter.BattleParams.TmpParam.CurrentTileIndex );
            _attackCharacter.GetTransformHandler.RotateToPosition( targetTileData.CharaStandPos );
            var attackerTileData = _stageCtrl.GetTileStaticData( _attackCharacter.BattleParams.TmpParam.CurrentTileIndex );
            _targetCharacter.GetTransformHandler.RotateToPosition( attackerTileData.CharaStandPos );

            // パラメータビューにキャラクターを割り当て
            var leftWindowLayerMaskIndex    = BattleRoutinePresenter.GetLayerMaskIndexFromWinType( ParameterWindowType.Left );
            var rightWindowLayerMaskIndex   = BattleRoutinePresenter.GetLayerMaskIndexFromWinType( ParameterWindowType.Right );
            _presenter.CharaParamView( ParameterWindowType.Left ).AssignCharacter( _targetCharacter, leftWindowLayerMaskIndex );
            _presenter.CharaParamView( ParameterWindowType.Right ).AssignCharacter( _attackCharacter, rightWindowLayerMaskIndex );
            _presenter.CharaParamView( ParameterWindowType.Left ).SetActive( true );
            _presenter.CharaParamView( ParameterWindowType.Right ).SetActive( true );

            _phase = EmAttackPhase.EM_ATTACK_CONFIRM;
        }

        public override bool Update()
        {
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

                    break;
                case EmAttackPhase.EM_ATTACK_EXECUTE:
                    {
                        _phase = EmAttackPhase.EM_ATTACK_END;
                    }
                    break;
                case EmAttackPhase.EM_ATTACK_END:
                    // 攻撃したキャラクターの攻撃コマンドを選択不可にする
                    _attackCharacter.BattleParams.TmpParam.SetEndCommandStatus( COMMAND_TAG.ATTACK, true );

                    Back();

                    return true;
            }

            return false;
        }

        public override object ExitState()
        {
            //死亡判定を通知(相手のカウンターによって倒される可能性もあるため、攻撃者と被攻撃者の両方を判定)
            Character diedCharacter = null;
            if( diedCharacter != null )
            {
                var key = new CharacterKey( diedCharacter.GetStatusRef.characterTag, diedCharacter.GetStatusRef.characterIndex );
                NorifyCharacterDied( key );
                // 破棄
                diedCharacter.Dispose();
            }

            // アタッカーキャラクターの設定を解除
            _stageCtrl.UnbindGridCursor();
            // 予測ダメージをリセット
            _attackCharacter.BattleParams.TmpParam.SetExpectedHpChange( 0, 0 );
            _targetCharacter.BattleParams.TmpParam.SetExpectedHpChange( 0, 0 );
            // アタックカーソルUI非表示
            _uiSystem.BattleUi.SetActiveLeft2RightDirection( false );
            // ダメージ予測表示UIを非表示
            _uiSystem.BattleUi.SetActiveActionResultExpect( false );
            // タイルメッシュの描画をすべてクリア
            _btlRtnCtrl.BtlCharaCdr.ClearAllTileMeshes();
            // 選択グリッドを表示
            // ※この攻撃の直後にプレイヤーフェーズに移行した場合、一瞬の間、選択グリッドが表示され、
            //   その後プレイヤーに選択グリッドが移るという状況になります。
            //   その挙動が少しバグのように見えてしまうので、消去したままにすることにし、
            //   次のキャラクターが行動開始する際に表示するようにします。
            // Stage.StageController.Instance.SetActiveGridCursor(true);

            return base.ExitState();
        }

        public override void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            _inputFcd.RegisterInputCodes(
               (GuideIcon.CONFIRM, "Confirm", CanAcceptConfirm, new AcceptContextInput( AcceptConfirm ), 0.0f, hashCode),
               (GuideIcon.SUB1, _targetCharaSkillNames[0], CanAcceptSub1, new AcceptContextInput( AcceptSub1 ), 0.0f, hashCode),
               (GuideIcon.SUB2, _targetCharaSkillNames[1], CanAcceptSub2, new AcceptContextInput( AcceptSub2 ), 0.0f, hashCode),
               (GuideIcon.SUB3, _targetCharaSkillNames[2], CanAcceptSub3, new AcceptContextInput( AcceptSub3 ), 0.0f, hashCode),
               (GuideIcon.SUB4, _targetCharaSkillNames[3], CanAcceptSub4, new AcceptContextInput( AcceptSub4 ), 0.0f, hashCode)
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

        protected override bool CanAcceptSub1() => CanAcceptSub( 0 );
        protected override bool CanAcceptSub2() => CanAcceptSub( 1 );
        protected override bool CanAcceptSub3() => CanAcceptSub( 2 );
        protected override bool CanAcceptSub4() => CanAcceptSub( 3 );
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

            _stageCtrl.SetActiveGridCursor( false );                            // 選択グリッドを一時非表示
            _uiSystem.BattleUi.SetActiveRight2LeftDirection( false );           // アタックカーソルUI非表示
            _uiSystem.BattleUi.SetActiveActionResultExpect( false );            // ダメージ予測表示UIを非表示
            _btlRtnCtrl.BtlCharaCdr.ClearAllTileMeshes();                       // タイルメッシュの描画をすべてクリア

            UnregisterInputCodes( Hash.GetStableHash( GetType().Name ) );       // 現在の入力コードを登録解除

            _sequenceFcd.RegistAttack( _attackCharacter, _targetCharacter );    // 攻撃シーケンス処理に移行

            _phase = EmAttackPhase.EM_ATTACK_EXECUTE;

            return true;
        }

        protected override bool AcceptSub1( InputContext context ) => AcceptSub( 0, context );
        protected override bool AcceptSub2( InputContext context ) => AcceptSub( 1, context );
        protected override bool AcceptSub3( InputContext context ) => AcceptSub( 2, context );
        protected override bool AcceptSub4( InputContext context ) => AcceptSub( 3, context );

        private bool CanAcceptSub( int index )
        {
            if( !CanAcceptDefault() )                       { return false; }
            if( EmAttackPhase.EM_ATTACK_CONFIRM != _phase ) { return false; }
            if( _targetCharaSkillNames[index].Length <= 0 ) { return false; }
            if( _targetCharacter is not Player )            { return false; }

            return _targetCharacter.BattleParams.TmpParam.IsUseableSkill[index];
        }

        private bool AcceptSub( int index, InputContext context )
        {
            if( !AccespuSubs[index]( context ) ) { return false; }

            _targetCharacter.BattleLogic.ToggleEquipSkill( index );

            return true;
        }
    }
}