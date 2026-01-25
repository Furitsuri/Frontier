using Zenject;
using Frontier.StateMachine;

namespace Frontier.FormTroop
{
    public class RecruitmentPhasePresenter : PhasePresenterBase
    {
        [Inject] private UserDomain _userDomain = null; 

        private RecruitmentUISystem _recruitmentUI = null;

        public void Init()
        {
            _recruitmentUI = _uiSystem.RecruitmentUi;

            _recruitmentUI.Init();
        }

        public void Update()
        {
            _recruitmentUI.SetMoneyValue( _userDomain.Money );  // 所持金の更新
        }

        public void Exit()
        {
            _recruitmentUI.Exit();
        }
    }
}