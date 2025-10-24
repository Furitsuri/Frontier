using Frontier.Battle;

namespace Frontier.StateMachine
{
    public class PlPhaseStateAnimation : PhaseAnimationStateBase
    {
        override protected void StartPhaseAnim()
        {
            _btlUi.TogglePhaseUI( true, TurnType.PLAYER_TURN );

            base.StartPhaseAnim();
        }
    }
}