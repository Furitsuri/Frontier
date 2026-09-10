using Zenject;

namespace Frontier.StateMachine
{
    public class PhasePresenterBase
    {
        [Inject] protected IUiSystem _uiSystem = null;
    }
}