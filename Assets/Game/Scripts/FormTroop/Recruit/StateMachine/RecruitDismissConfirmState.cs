using Frontier.StateMachine;
using Zenject;

namespace Frontier.FormTroop
{
    /// <summary>
    /// 解雇対象メンバーを解雇してよいかを確認するステート(RecruitDismissStateの子)。
    /// </summary>
    public sealed class RecruitDismissConfirmState : ConfirmPhaseStateBase
    {
        [Inject] private ILocalizationService _localization = null;

        private int _targetIndex = -1;

        public override void Init( object context )
        {
            base.Init( context );

            ReceiveContext( ref _targetIndex, context );

            ( _confirmPresenter as RecruitPhasePresenter )?.SetConfirmMessage( _localization.Get( LocKey.UI_CONFIRM_DISMISS_MEMBER_MESSAGE ) );
        }

        protected override bool AcceptConfirm( InputContext context )
        {
            if( !base.AcceptConfirm( context ) ) { return false; }

            if( _commandList.GetCurrentValue() == ( int ) ConfirmTag.YES )
            {
                GetParent<RecruitDismissState>()?.RequestDismiss( _targetIndex );
            }

            Back();

            return true;
        }
    }
}
