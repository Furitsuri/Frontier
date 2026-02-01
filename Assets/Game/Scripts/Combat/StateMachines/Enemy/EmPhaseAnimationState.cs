using Frontier.StateMachine;

namespace Frontier.Battle
{
    public class EmPhaseStateAnimation : PhaseAnimationStateBase
    {
        protected override void StartPhaseAnim()
        {
            _btlUi.TogglePhaseUI( true, TurnType.ENEMY_TURN );

            base.StartPhaseAnim();
        }
    }
}