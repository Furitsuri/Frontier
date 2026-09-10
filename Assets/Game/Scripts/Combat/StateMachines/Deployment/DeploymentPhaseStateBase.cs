using Frontier.Stage;
using Frontier.StateMachine;
using Zenject;

namespace Frontier.Battle
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