using Frontier.StateMachine;
using Zenject;

namespace Frontier.Battle
{
    /// <summary>
    /// 移動先が、暫定的に移動している他キャラクターの「移動前の位置」である場合に表示する確認ステートです。
    /// そのまま移動すると、対象のキャラクターは移動前の位置へ戻せなくなるため、その旨を伝えて可否を選択させます。
    /// 対象キャラクター名の配列(string[])を遷移コンテキストとして受け取り、選択結果は Confirmed で
    /// 呼び出し元(PlMoveState/PlGroupMoveState)に伝えます。
    /// </summary>
    public sealed class PlConfirmBlockUndoMoveState : ConfirmPhaseStateBase
    {
        private const string NAME_SEPARATOR = ", ";

        [Inject] private ILocalizationService _localization = null;

        private string[] _blockedCharacterNames = null;

        public bool Confirmed { get; private set; } = false;

        public override void Init( object context )
        {
            base.Init( context );

            Confirmed = false;

            _blockedCharacterNames = null;
            ReceiveContext( ref _blockedCharacterNames, context );

            string names = ( null != _blockedCharacterNames ) ? string.Join( NAME_SEPARATOR, _blockedCharacterNames ) : string.Empty;
            ( _confirmPresenter as BattleRoutinePresenter )?.SetConfirmMessage(
                string.Format( _localization.Get( LocKey.UI_CONFIRM_BLOCK_UNDO_MOVE_MESSAGE ), names ) );
        }

        protected override bool AcceptConfirm( InputContext context )
        {
            if( !base.AcceptConfirm( context ) ) { return false; }

            Confirmed = ( _commandList.GetCurrentValue() == ( int ) ConfirmTag.YES );
            Back();

            return true;
        }
    }
}
