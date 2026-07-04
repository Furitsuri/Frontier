
using Frontier.StateMachine;

namespace Frontier.FormTroop
{
    /// <summary>
    /// 雇用を行わずにRecruitルーチンから脱出してよいかを確認するステート
    /// </summary>
    public sealed class RecruitConfirmCancelState : ConfirmPhaseStateBase
    {
        public override void Init( object context )
        {
            base.Init( context );

            ( _confirmPresenter as RecruitPhasePresenter )?.SetConfirmMessage( "cancel recruiting?" );
        }

        protected override bool AcceptConfirm( InputContext context )
        {
            if( !base.AcceptConfirm( context ) ) { return false; }

            if( _commandList.GetCurrentValue() == ( int ) ConfirmTag.YES )
            {
                GetParent<RecruitRootState>()?.RequestCancelExit();
                _isEndedPhase = true;
            }

            Back();

            return true;
        }
    }
}
