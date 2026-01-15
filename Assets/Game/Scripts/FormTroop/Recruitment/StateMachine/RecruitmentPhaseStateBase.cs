using Frontier.StateMachine;

namespace Frontier.FormTroop
{
    public class RecruitmentPhaseStateBase : PhaseStateBase
    {
        protected RecruitmentPhasePresenter _presenter = null;

        public override void AssignPresenter( PhasePresenterBase presenter )
        {
            _presenter = presenter as RecruitmentPhasePresenter;
        }
    }
}