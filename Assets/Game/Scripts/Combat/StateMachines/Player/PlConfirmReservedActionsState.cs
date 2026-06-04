using Frontier.Combat;
using Frontier.Entities;
using Frontier.Sequences;
using Frontier.Stage;
using Frontier.StateMachine;
using System.Collections.Generic;
using Zenject;

namespace Frontier.Battle
{
    /// <summary>
    /// キュー積みされたスキルアクションが残存する状態でプレイヤーフェーズが終了しようとした際に
    /// 確認画面を表示し、YES 選択時にキュー内のアクションをすべて順番に実行します。
    /// </summary>
    public class PlConfirmReservedActionsState : ConfirmPhaseStateBase
    {
        private enum ExecutionPhase { CONFIRM, EXECUTING }

        [Inject] private BattleRoutineController     _btlRtnCtrl      = null;
        [Inject] private SkillActionReservationQueue _reservationQueue = null;
        [Inject] private SequenceFacade              _sequenceFcd      = null;
        [Inject] private StageController             _stageCtrl        = null;

        private ExecutionPhase _execPhase;
        private Character      _lastExecutedAttacker;

        public override void Init( object context )
        {
            base.Init( context );

            _execPhase            = ExecutionPhase.CONFIRM;
            _lastExecutedAttacker = null;

            ( _confirmPresenter as BattleRoutinePresenter )?.SetConfirmMessage(
                "There are queued actions that have not been executed.\nExecute all and transition to the enemy phase?" );
        }

        public override bool Update()
        {
            if( _execPhase == ExecutionPhase.EXECUTING )
            {
                if( !_sequenceFcd.IsEmptySequence() ) { return IsBack(); }

                _lastExecutedAttacker?.BattleParams.TmpParam.SetEndCommandStatus( COMMAND_TAG.SKILL, true );

                if( !_reservationQueue.IsEmpty )
                {
                    ExecuteNextQueuedAction();
                }
                else
                {
                    _btlRtnCtrl.BtlCharaCdr.ApplyAllArmyEndAction( CHARACTER_TAG.PLAYER );
                    Back();
                }

                return IsBack();
            }

            return base.Update();
        }

        protected override bool CanAcceptDefault()
        {
            if( _execPhase == ExecutionPhase.EXECUTING ) { return false; }
            return base.CanAcceptDefault();
        }

        protected override bool AcceptConfirm( InputContext context )
        {
            if( !base.AcceptConfirm( context ) ) { return false; }

            if( _commandList.GetCurrentValue() == ( int ) ConfirmTag.YES )
            {
                _confirmPresenter.SetActiveConfirmUI( false );
                _execPhase = ExecutionPhase.EXECUTING;

                _btlRtnCtrl.BtlCharaCdr.ClearAllTileMeshesAndGhosts();

                ExecuteNextQueuedAction();
            }
            else
            {
                Back();
            }

            return true;
        }

        private void ExecuteNextQueuedAction()
        {
            var data     = _reservationQueue.Dequeue();
            var attacker = _btlRtnCtrl.BtlCharaCdr.GetCharacter( data.AttackerKey );
            if( attacker == null ) { return; }

            _lastExecutedAttacker = attacker;
            _stageCtrl.ApplyGridCursor2CharacterTileWithFocusCamera( attacker );

            Character target = null;
            if( data.FocusedTargetCharaKey.IsValid() )
            {
                target = _btlRtnCtrl.BtlCharaCdr.GetCharacter( data.FocusedTargetCharaKey );
            }

            // ゴーストを使用するスキル（ダッシュ斬りなど）のためにゴーストを再構築する。
            // ExitState で CleanupGhost が呼ばれているため、キュー実行時点ではゴーストが存在しない。
            if( data.GhostTileIndex >= 0 )
            {
                var ghost = attacker.GetGhostObject();
                ghost.TileIndex              = data.GhostTileIndex;
                ghost.transform.position     = _stageCtrl.GetTileStaticData( data.GhostTileIndex ).CharaStandPos;
            }

            for( int i = 0; i < data.AttackerSkillsToggledON.Length; ++i )
            {
                attacker.BattleParams.TmpParam.IsSkillsToggledON[i] = data.AttackerSkillsToggledON[i];
            }
            attacker.BattleParams.TmpParam.ActGaugeConsumption = data.ActGaugeConsumption;

            attacker.BattleLogic.ActionRangeCtrl.ActionableRangeRdr.ClearTileMeshesByType( TileMapType.QUEUED );

            attacker.BattleLogic.ConsumeActionGaugeForSkill();
            if( target != null ) { target.BattleLogic.ConsumeActionGauge(); }

            if( attacker.BattleLogic.RegistSelfBuffSequences() )
            {
                attacker.RefreshUseableSkillFlags( SituationType.ATTACK, Methods.ToBit( ActionType.BUFF ) );
            }

            var attackTargetKeys = new List<CharacterKey>( data.AttackTargetCharaKeys );
            _sequenceFcd.RegistSkillAction( attacker, target, data.UseSkillID, attackTargetKeys );
        }
    }
}