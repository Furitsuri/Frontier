
using Frontier.Combat;
using Frontier.Entities;
using Frontier.Sequences;
using Frontier.Stage;
using Frontier.UI;
using System.Collections.Generic;
using Zenject;
using static Constants;

namespace Frontier.Battle
{
    /// <summary>
    /// スキルを予約済みのキャラクターを選択した際、その場で予約中のスキルアクションを即時実行するステートです。
    /// </summary>
    public class PlExecuteReservedActionState : PlPhaseStateBase
    {
        private enum ExecutionPhase
        {
            EXECUTING,
            END,
        }

        [Inject] private SequenceFacade _sequenceFcd                   = null;
        [Inject] private SkillActionReservationQueue _reservationQueue = null;

        private Character _target;
        private ExecutionPhase _phase;

        public override void Init( object context )
        {
            base.Init( context );

            _target = null;

            // 移動を伴うスキルの経路表示矢印は、即時実行を選んだ時点で不要になるため消去する
            _plOwner.BattleLogic.ActionRangeCtrl.ClearMoveDirectionArrows();

            if( !_reservationQueue.TryDequeueByAttackerKey( _plOwner.GetCharacterKey(), out var data ) )
            {
                _phase = ExecutionPhase.END;
                Back();
                return;
            }

            if( data.FocusedTargetCharaKey.IsValid() )
            {
                _target = _btlRtnCtrl.BtlCharaCdr.GetCharacter( data.FocusedTargetCharaKey );
            }

            // ゴーストを使用するスキル（ダッシュ斬りなど）のためにゴーストを再構築する
            if( data.GhostTileIndex >= 0 )
            {
                var ghost = _plOwner.GetGhostObject();
                ghost.TileIndex          = data.GhostTileIndex;
                ghost.transform.position = _stageCtrl.GetTileStaticData( data.GhostTileIndex ).CharaStandPos;
            }

            for( int i = 0; i < data.AttackerSkillsToggledON.Length; ++i )
            {
                _plOwner.BattleParams.TmpParam.IsSkillsToggledON[i] = data.AttackerSkillsToggledON[i];
            }
            _plOwner.BattleParams.TmpParam.ActGaugeConsumption = data.ActGaugeConsumption;

            _plOwner.BattleLogic.ActionRangeCtrl.ActionableRangeRdr.ClearTileMeshesByType( TileMapType.QUEUED );

            _plOwner.BattleLogic.ConsumeActionGaugeForSkill();
            if( _target != null ) { _target.BattleLogic.ConsumeActionGauge(); }

            if( _plOwner.BattleLogic.RegistSelfBuffSequences() )
            {
                _plOwner.RefreshUseableSkillFlags( SituationType.ATTACK, Methods.ToBit( ActionType.BUFF ) );
            }

            var attackTargetKeys = new List<CharacterKey>( data.AttackTargetCharaKeys );
            _sequenceFcd.RegistSkillAction( _plOwner, _target, data.UseSkillID, attackTargetKeys );

            _phase = ExecutionPhase.EXECUTING;
        }

        public override bool Update()
        {
            if( base.Update() ) { return true; }

            if( _phase == ExecutionPhase.EXECUTING )
            {
                // スキルアクションが完了するまでは行動終了状態にしない(完了前にグレー化してしまうのを防ぐ)
                if( !_sequenceFcd.IsEmptySequence() ) { return IsBack(); }

                _plOwner.BattleParams.TmpParam.SetEndCommandStatus( COMMAND_TAG.SKILL, true );
                _plOwner.ClearCommandHistory();

                _phase = ExecutionPhase.END;
                Back();

                return true;
            }

            return false;
        }

        // 何もしない( base.OnActivatedを呼ばない )
        // Back()呼び出しがOnActivatedのデフォルト処理で打ち消されてしまうため
        protected override void OnActivated() { }

        public override object ExitState()
        {
            OnExitStateAfterCombat( _plOwner, _target );

            return base.ExitState();
        }

        /// <summary>
        /// 操作対象のプレイヤーを設定します
        /// </summary>
        protected override void AdaptSelectPlayer()
        {
            var selectCharacter = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();
            _plOwner            = _btlRtnCtrl.BtlCharaCdr.GetPlayer( selectCharacter.GetCharacterKey() );
            NullCheck.AssertNotNull( _plOwner, nameof( _plOwner ) );
        }
    }
}
