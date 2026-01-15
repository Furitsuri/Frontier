using Frontier.Stage;
using Frontier.StateMachine;
using Zenject;

namespace Frontier.Battle
{
    public class TroopPhaseHandler : PhaseHandlerBase
    {
        [Inject] protected BattleRoutineController _btlRtnCtrl = null;
        [Inject] protected BattleRoutinePresenter _presenter = null;
        [Inject] protected StageController _stgCtrl = null;
    }
}