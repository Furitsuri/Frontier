
using Frontier.StateMachine;

namespace Frontier.FormTroop
{
    public class RecruitConfirmCompletedState : ConfirmPhaseStateBase
    {
        protected override bool AcceptConfirm( bool isInput )
        {
            if( !isInput ) { return false; }

            if( _commandList.GetCurrentValue() == ( int ) ConfirmTag.YES )
            {
                _isEndedPhase = true;
            }

            Back();

            return true;
        }
    }
}