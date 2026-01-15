using Zenject;
using Frontier.StateMachine;

namespace Frontier.FormTroop
{
    public class RecruitmentPhasePresenter : PhasePresenterBase
    {
        private RecruitmentUISystem _recruitmentUI = null;

        public void Init()
        {
            _recruitmentUI = _uiSystem.RecruitmentUi;
        }

        public void Update()
        {
        }

        public void Exit()
        {
        }
    }
}