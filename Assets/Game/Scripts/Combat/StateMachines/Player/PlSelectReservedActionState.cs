
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
    /// スキルを予約済みのキャラクターを選択した際に表示する、予約に対する操作(即時実行等)を選び、
    /// そのまま実行までを行うステートです。
    /// PlAttackStateやPlSkillActionToTargetStateと同様、フェーズをまたいでも同一ステート内に留まることで、
    /// SequenceFacadeによる実行が終わるまでCanAcceptDefaultが入力を拒否し、入力ガイドの表示も自動的に消えます。
    /// </summary>
    public class PlSelectReservedActionState : PlPhaseStateBase, ICommandCursorProvider
    {
        private enum Phase
        {
            SELECT_OPTION = 0,
            EXECUTING,
            END,
        }

        [Inject] private SequenceFacade _sequenceFcd                   = null;

        private CommandList _commandList = new CommandList();
        private CommandList.CommandIndexedValue _cmdIdxVal;
        private Phase _phase;
        private Character _target;

        public override void Init( object context )
        {
            base.Init( context );

            _phase  = Phase.SELECT_OPTION;
            _target = null;

            _cmdIdxVal = new CommandList.CommandIndexedValue( 0, 0 );

            var options = GetOptions();
            var commandIndices = new List<int>();
            foreach( var option in options )
            {
                commandIndices.Add( ( int ) option );
            }
            _commandList.Init( ref commandIndices, CommandList.CommandDirection.VERTICAL, false, _cmdIdxVal );

            _presenter.InitReservedActionOptionView( this, options );
        }

        public override bool Update()
        {
            if( base.Update() ) { return true; }

            switch( _phase )
            {
                case Phase.SELECT_OPTION:
                    break;
                case Phase.EXECUTING:
                    // スキルアクションが完了するまでは行動終了状態にしない(完了前にグレー化してしまうのを防ぐ)
                    if( !_sequenceFcd.IsEmptySequence() ) { break; }

                    _plOwner.FinalizeCommand( COMMAND_TAG.SKILL );

                    _phase = Phase.END;
                    break;
                case Phase.END:
                    Back();
                    return true;
            }

            return false;
        }

        public override object ExitState()
        {
            _presenter.ExitReservedActionOptionView();

            // 実行フェーズまで進んでいた場合のみ、対象選択の後始末(カーソル復帰、カメラフォーカス等)を行う
            if( _phase != Phase.SELECT_OPTION )
            {
                CleanupTargetSelectionState( _plOwner, _target );
            }

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

        public override void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            _inputFcd.RegisterInputCodes(
                (GuideIcon.VERTICAL_CURSOR, "Select",  CanAcceptDefault, new AcceptContextInput( AcceptDirection ), MENU_DIRECTION_INPUT_INTERVAL, hashCode),
                (GuideIcon.CONFIRM,         "Confirm", CanAcceptDefault, new AcceptContextInput( AcceptConfirm ),   0.0f, hashCode),
                (GuideIcon.CANCEL,          "Back",    CanAcceptDefault, new AcceptContextInput( AcceptCancel ),    0.0f, hashCode)
            );
        }

        /// <summary>
        /// 選択肢実行フェーズに入ってからは、カメラ操作とデバッグ遷移以外の入力を受け付けない
        /// </summary>
        protected override bool CanAcceptDefault()
        {
            if( _phase != Phase.SELECT_OPTION ) { return false; }
            return base.CanAcceptDefault();
        }

        public int GetCurrentIndex() => _cmdIdxVal.index;

        protected override bool AcceptDirection( InputContext context )
        {
            return _commandList.OperateListCursor( context.Cursor );
        }

        protected override bool AcceptConfirm( InputContext context )
        {
            if( !base.AcceptConfirm( context ) ) { return false; }

            ExecuteSelectedOption( ( RESERVED_ACTION_OPTION_TAG ) _cmdIdxVal.value );

            return true;
        }

        private void ExecuteSelectedOption( RESERVED_ACTION_OPTION_TAG option )
        {
            switch( option )
            {
                case RESERVED_ACTION_OPTION_TAG.EXECUTE:
                    ExecuteReservedAction();
                    break;
            }
        }

        /// <summary>
        /// 予約中のスキルアクションをその場で即時実行します
        /// </summary>
        private void ExecuteReservedAction()
        {
            // 移動を伴うスキルの経路表示矢印は、即時実行を選んだ時点で不要になるため消去する
            _plOwner.BattleLogic.ActionRangeCtrl.ClearMoveDirectionArrows();

            if( !_reservationQueue.TryDequeueByAttackerKey( _plOwner.GetCharacterKey(), out var data ) )
            {
                _phase = Phase.END;
                return;
            }

            _target = ReservedSkillActionApplier.Apply( data, _plOwner, _stageCtrl, _btlRtnCtrl.BtlCharaCdr );

            var attackTargetKeys = new List<CharacterKey>( data.AttackTargetCharaKeys );
            _sequenceFcd.RegistSkillAction( _plOwner, _target, data.UseSkillID, attackTargetKeys );

            _phase = Phase.EXECUTING;
        }

        private List<RESERVED_ACTION_OPTION_TAG> GetOptions()
        {
            return new List<RESERVED_ACTION_OPTION_TAG>
            {
                RESERVED_ACTION_OPTION_TAG.EXECUTE,
            };
        }
    }
}
