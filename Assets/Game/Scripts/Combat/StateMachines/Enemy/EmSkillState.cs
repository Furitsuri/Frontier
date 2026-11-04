using Frontier.Combat;
using Frontier.Entities;
using Frontier.Entities.Ai;
using Frontier.Sequences;
using Frontier.Stage;
using Frontier.UI;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.Battle
{
    /// <summary>
    /// 敵がスキルによる攻撃を行うステートです。
    /// 使用するスキル・向き・攻撃対象などは、AIが選択した攻撃(AttackOption)に従います
    /// </summary>
    public class EmSkillState : UnitPhaseState
    {
        private enum EmSkillPhase
        {
            EM_SKILL_CONFIRM = 0,
            EM_SKILL_EXECUTE,
            EM_SKILL_END,
        }

        [Inject] private SequenceFacade _sequenceFcd = null;

        private EmSkillPhase _phase;
        private Enemy _attackCharacter      = null;
        private Character _targetCharacter  = null;
        private AttackOption _attackOption;

        public override void Init( object context )
        {
            base.Init( context );

            _attackCharacter = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter() as Enemy;
            Debug.Assert( _attackCharacter != null );

            bool isSelected = _attackCharacter.BattleLogic.GetAi().TryGetAttackOption( out _attackOption );
            Debug.Assert( isSelected && !_attackOption.IsNormalAttack, "スキルによる攻撃が選択されていない状態でEmSkillStateに遷移しています。" );

            _targetCharacter = _attackOption.MainTarget;
            var skillData    = SkillsData.data[( int ) _attackOption.SkillID];

            // スキルを使用ONにして、スキルの効果(攻撃回数・倍率・消費ゲージ等)を適用する
            int slotIndex = GetEquipSkillSlotIndex( _attackCharacter, _attackOption.SkillID );
            Debug.Assert( 0 <= slotIndex, "使用するスキルを装備していません。" );
            _attackCharacter.BattleLogic.ToggleEquipSkill( slotIndex );

            // スキルの攻撃範囲を設定して表示する
            var actionRangeCtrl = _attackCharacter.BattleLogic.ActionRangeCtrl;
            int dprtTileIndex   = _attackCharacter.BattleParams.TmpParam.CurrentTileIndex;
            actionRangeCtrl.SetupAttackableRangeData( dprtTileIndex, _attackOption.SkillID );

            if( TargetingMode.DIRECTIONAL == skillData.TargetingMode )
            {
                // 選択した向きへ回転させた上で、プレイヤーの対象選択と同じ処理で対象範囲・着地先(ゴースト)を表示する
                // (NPCは射程を調整せず、常に最大の射程で使用する)
                _attackCharacter.OrderRotate( Quaternion.Euler( 0f, StageDirectionConverter.DirectionAngles[( int ) _attackOption.Direction], 0f ) );
                actionRangeCtrl.RefreshTargetableRange( TargetingMode.DIRECTIONAL, true, skillData.IsMovingSkill, dprtTileIndex, actionRangeCtrl.SkillMaxRange );

                if( skillData.IsMovingSkill && _attackCharacter.GhostObj?.TileIndex != _attackOption.LandingTileIndex )
                {
                    Debug.LogWarning( $"敵のスキルの着地先が、AIが選択した着地先と異なります。(選択:{_attackOption.LandingTileIndex}, 実際:{_attackCharacter.GhostObj?.TileIndex})" );
                }
            }
            else
            {
                actionRangeCtrl.DrawAttackableRange();

                // 攻撃者と主となる攻撃対象を向かい合わせる
                var targetTileData = _stageCtrl.GetTileStaticData( _targetCharacter.BattleParams.TmpParam.CurrentTileIndex );
                _attackCharacter.RotateToPosition( targetTileData.CharaStandPos );
            }

            // アタッカーキャラクターの設定
            _stageCtrl.BindGridCursor( GridCursorState.ATTACK, _attackCharacter );
            _stageCtrl.ApplyGridCursor2CharacterTile( _attackCharacter );
            // MEMO : カメラの注視先はターゲットカーソルへ切り替えない。
            //        敵の行動後はグリッドカーソルへ注視先を戻さない(CleanupTargetSelectionStateでfocusOwner:false)ため、
            //        切り替えるとプレイヤーフェーズになってもカメラがグリッドカーソルに追従しなくなる
            _stageCtrl.ApplyTargetCursor2CharacterTile( false, _targetCharacter );

            // 対象選択中はカメラを少しだけ近づける
            _btlRtnCtrl.GetBtlCameraCtrl.SetTargetSelectZoomActive( true );

            // パラメータビューにキャラクターを割り当て
            var leftWindowLayerMaskIndex    = BattleRoutinePresenter.GetLayerMaskIndexFromWinType( ParameterWindowType.Left );
            var rightWindowLayerMaskIndex   = BattleRoutinePresenter.GetLayerMaskIndexFromWinType( ParameterWindowType.Right );
            _presenter.CharaParamView( ParameterWindowType.Left ).AssignCharacter( _targetCharacter, leftWindowLayerMaskIndex );
            _presenter.CharaParamView( ParameterWindowType.Right ).AssignCharacter( _attackCharacter, rightWindowLayerMaskIndex );
            _presenter.CharaParamView( ParameterWindowType.Left ).SetActive( true );
            _presenter.CharaParamView( ParameterWindowType.Right ).SetActive( true );
            _presenter.SetActiveActionResultExpect( true, ParameterWindowType.Right );

            // 予測ダメージを適応し、攻撃対象全員のHPゲージに予測ダメージ分の点滅を反映する
            _btlRtnCtrl.BtlCharaCdr.ApplyDamageExpect( _attackCharacter, _targetCharacter );
            var targetingContext = new TargetingRangeContext { BtlRtnCtrl = _btlRtnCtrl, Presenter = _presenter, Owner = _attackCharacter, StageCtrl = _stageCtrl };
            TargetingPreviewHelper.RefreshPredictedDamageGauges( targetingContext, _attackOption.AttackTargetKeys );

            _phase = EmSkillPhase.EM_SKILL_CONFIRM;
        }

        public override bool Update()
        {
            switch( _phase )
            {
                case EmSkillPhase.EM_SKILL_CONFIRM:
                    break;
                case EmSkillPhase.EM_SKILL_EXECUTE:
                    _phase = EmSkillPhase.EM_SKILL_END;
                    break;
                case EmSkillPhase.EM_SKILL_END:
                    // スキルのシーケンスが終了したため、左右のパラメータウィンドウを閉じる
                    // (敵フェーズにはタイル選択によるパラメータ表示の自動更新が無く、閉じなければ次の行動まで表示が残るため)
                    CloseParameterWindowAfterSequence( ParameterWindowType.Left );
                    CloseParameterWindowAfterSequence( ParameterWindowType.Right );
                    // 攻撃またはスキルを実行した時点で行動終了とするため、攻撃・スキルの両コマンドを選択不可にする
                    _attackCharacter.BattleParams.TmpParam.SetEndCommandStatus( COMMAND_TAG.ATTACK, true );
                    _attackCharacter.BattleParams.TmpParam.SetEndCommandStatus( COMMAND_TAG.SKILL, true );

                    Back();

                    return true;
            }

            return false;
        }

        public override object ExitState()
        {
            // 対象選択の共通後始末
            // (パラメータウィンドウはスキルの演出中も表示し続けるため非表示にせず、グリッドカーソルもEmAttackStateと同様の理由でオーナーへ戻さない)
            CleanupTargetSelectionState( _attackCharacter, _targetCharacter, ParameterWindowType.Right, hideTargetWindow: false, focusOwner: false );
            // 他キャラクターの描画分も含め、タイルメッシュの描画とゴーストをすべてクリア
            _btlRtnCtrl.BtlCharaCdr.ClearAllTileMeshesAndGhosts();

            return base.ExitState();
        }

        public override void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            _inputFcd.RegisterInputCodes(
               (GuideIcon.CONFIRM, "Confirm", CanAcceptConfirm, new AcceptContextInput( AcceptConfirm ), 0.0f, hashCode)
            );
        }

        /// <summary>
        /// 決定入力受付の可否を判定します
        /// </summary>
        /// <returns>決定入力受付の可否</returns>
        protected override bool CanAcceptConfirm()
        {
            if( !CanAcceptDefault() ) return false;

            return EmSkillPhase.EM_SKILL_CONFIRM == _phase;
        }

        /// <summary>
        /// 決定入力を受け取った際の処理を行います
        /// </summary>
        protected override bool AcceptConfirm( InputContext context )
        {
            if( !base.AcceptConfirm( context ) ) { return false; }

            // キャラクターのアクションゲージを消費
            _attackCharacter.BattleLogic.ConsumeActionGaugeForSkill();
            _targetCharacter.BattleLogic.ConsumeActionGauge();

            FinalizeTargetSelection( ParameterWindowType.Right );
            _attackCharacter.BattleLogic.ActionRangeCtrl.ActionableRangeRdr.ClearTileMeshesByType( TileMapType.ATTACKABLE | TileMapType.TARGETABLE | TileMapType.QUEUED );

            UnregisterInputCodes( Hash.GetStableHash( GetType().Name ) );   // 現在の入力コードを登録解除

            _sequenceFcd.RegistSkillAction( _attackCharacter, _targetCharacter, _attackOption.SkillID, _attackOption.AttackTargetKeys );    // スキルのシーケンス処理に移行

            _phase = EmSkillPhase.EM_SKILL_EXECUTE;

            return true;
        }

        /// <summary>
        /// 指定のスキルを装備しているスロットのうち、未使用のスロットの番号を取得します
        /// </summary>
        /// <returns>スロットの番号。存在しない場合は-1</returns>
        private int GetEquipSkillSlotIndex( Character chara, SkillID skillID )
        {
            for( int i = 0; i < EQUIPABLE_SKILL_MAX_NUM; ++i )
            {
                if( chara.GetEquipSkillID( i ) == skillID && !chara.BattleParams.TmpParam.IsSkillsUsed[i] ) { return i; }
            }

            return -1;
        }
    }
}
