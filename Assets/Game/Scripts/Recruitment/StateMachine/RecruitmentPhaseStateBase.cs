using Frontier.StateMachine;

namespace Frontier.Recruitment
{
    public class RecruitmentPhaseStateBase : PhaseStateBase
    {
        protected RecruitmentPhasePresenter _presenter = null;

        override public void AssignPresenter( PhasePresenterBase presenter )
        {
            _presenter = presenter as RecruitmentPhasePresenter;
        }
    }
}