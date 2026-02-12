using Frontier.StateMachine;

namespace Frontier.Battle
{
    public class PlPhaseStateAnimation : PhaseAnimationStateBase
    {
        protected override void StartPhaseAnim()
        {
            _btlUi.SetTurnType( TurnType.PLAYER_TURN );

            base.StartPhaseAnim();
        }
    }
}