
using Frontier.StateMachine;

namespace Frontier.FormTroop
{
    public class RecruitConfirmCompletedState : ConfirmPhaseStateBase
    {
        protected override bool AcceptConfirm( InputContext context )
        {
            if( !base.AcceptConfirm( context ) ) { return false; }

            if( _commandList.GetCurrentValue() == ( int ) ConfirmTag.YES )
            {
                _isEndedPhase = true;
            }

            Back();

            return true;
        }
    }
}