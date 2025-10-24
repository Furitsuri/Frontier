using Frontier.Battle;

namespace Frontier.StateMachine
{
    public class EmPhaseStateAnimation : PhaseAnimationStateBase
    {
        override protected void StartPhaseAnim()
        {
            _btlUi.TogglePhaseUI( true, TurnType.ENEMY_TURN );

            base.StartPhaseAnim();
        }
    }
}