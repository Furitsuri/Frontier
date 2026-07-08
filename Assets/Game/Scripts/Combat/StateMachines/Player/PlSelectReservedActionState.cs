
using Frontier.Combat;
using Frontier.Entities;
using System.Collections.Generic;
using Zenject;
using static Constants;

namespace Frontier.Battle
{
    /// <summary>
    /// スキルを予約済みのキャラクターを選択した際に表示する、予約に対する操作(即時実行等)を選ぶステートです。
    /// PlSelectCommandStateと同様に、選択した項目の値をそのまま子ステートへの遷移先インデックスとして用います。
    /// </summary>
    public class PlSelectReservedActionState : PlPhaseStateBase, ICommandCursorProvider
    {
        [Inject] private SkillActionReservationQueue _reservationQueue = null;

        private CommandList _commandList = new CommandList();
        private CommandList.CommandIndexedValue _cmdIdxVal;

        public override void Init( object context )
        {
            base.Init( context );

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

        protected override void OnActivated()
        {
            base.OnActivated();

            // 子ステートで予約が実行済み(キューから取り除かれている)であれば、
            // このキャラクターの行動は完了しているためタイル選択ステートまで戻る
            if( !_reservationQueue.ContainsAttackerKey( _plOwner.GetCharacterKey() ) )
            {
                Back();
            }
        }

        public override object ExitState()
        {
            _presenter.ExitReservedActionOptionView();

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

        public int GetCurrentIndex() => _cmdIdxVal.index;

        protected override bool AcceptDirection( InputContext context )
        {
            return _commandList.OperateListCursor( context.Cursor );
        }

        protected override bool AcceptConfirm( InputContext context )
        {
            if( !base.AcceptConfirm( context ) ) { return false; }

            TransitStateWithExit( _cmdIdxVal.value );

            return true;
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
