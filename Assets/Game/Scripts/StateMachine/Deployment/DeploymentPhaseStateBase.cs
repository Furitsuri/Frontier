using Frontier.Battle;
using Frontier.Stage;
using Zenject;

namespace Frontier.StateMachine
{
    public class DeploymentPhaseStateBase : PhaseStateBase
    {
        [Inject] protected BattleRoutineController _btlRtnCtrl  = null;
        [Inject] protected StageController _stageCtrl           = null;

        protected DeploymentPhasePresenter _presenter = null;

        public override void AssignPresenter( PhasePresenterBase presenter )
        {
            _presenter = presenter as DeploymentPhasePresenter;
        }
    }
}