using Frontier.Combat;
using Frontier.Entities;
using Frontier.Sequences;
using Frontier.Stage;
using Frontier.StateMachine;
using Frontier.UI;
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

        private ExecutionPhase  _execPhase;
        private Player          _lastExecutedAttacker;

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

                // スキルのシーケンスが終了したため、攻撃対象側(右)のパラメータウィンドウを閉じる
                // (UnitPhaseStateを継承しないため、CloseParameterWindowと同じ処理を直接行う)
                ( _confirmPresenter as BattleRoutinePresenter )?.CharaParamView( ParameterWindowType.Right ).ClearCharacter();

                _lastExecutedAttacker ?.FinalizeCommand( COMMAND_TAG.SKILL );

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
                _confirmPresenter.SetActiveConfirmUI( false, UIType );
                _execPhase = ExecutionPhase.EXECUTING;

                _btlRtnCtrl.BtlCharaCdr.ClearAllTileMeshes();
                _btlRtnCtrl.BtlCharaCdr.ClearAllMoveDirectionArrows();
                HideQueuedAttackersGhosts();

                ExecuteNextQueuedAction();
            }
            else
            {
                Back();
            }

            return true;
        }

        /// <summary>
        /// 予約を実行するプレイヤー全員の、着地先のゴーストを非表示にします。
        /// MEMO : 全キャラクターを対象にしてはならない(敵が予兆として予約した着地先のゴーストまで消えてしまうため)
        /// </summary>
        private void HideQueuedAttackersGhosts()
        {
            foreach( var data in _reservationQueue.GetAll() )
            {
                _btlRtnCtrl.BtlCharaCdr.GetPlayer( data.AttackerKey )?.SetGhostActive( false );
            }
        }

        private void ExecuteNextQueuedAction()
        {
            var data     = _reservationQueue.Dequeue();
            var attacker = _btlRtnCtrl.BtlCharaCdr.GetPlayer( data.AttackerKey );
            if( attacker == null ) { return; }

            _lastExecutedAttacker = attacker;
            _stageCtrl.ApplyGridCursor2CharacterTileWithFocusCamera( attacker );

            // ゴーストは確定時に非表示にしている(ReservedSkillActionApplier.Apply内で、非表示のまま予約時の着地先を設定し直す)
            var target = ReservedSkillActionApplier.Apply( data, attacker, _stageCtrl, _btlRtnCtrl.BtlCharaCdr );

            var attackTargetKeys = new List<CharacterKey>( data.AttackTargetCharaKeys );
            _sequenceFcd.RegistSkillAction( attacker, target, data.UseSkillID, attackTargetKeys );
        }
    }
}