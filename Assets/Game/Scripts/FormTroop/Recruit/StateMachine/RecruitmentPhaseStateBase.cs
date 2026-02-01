using Frontier.StateMachine;

namespace Frontier.FormTroop
{
    public class RecruitPhaseStateBase : PhaseStateBase
    {
        protected RecruitPhasePresenter _presenter = null;

        public override void AssignPresenter( PhasePresenterBase presenter )
        {
            _presenter = presenter as RecruitPhasePresenter;
        }
    }
}