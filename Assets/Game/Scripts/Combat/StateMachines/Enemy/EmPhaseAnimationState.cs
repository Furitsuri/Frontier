using Frontier.StateMachine;

namespace Frontier.Battle
{
    public class EmPhaseStateAnimation : PhaseAnimationStateBase
    {
        protected override void StartPhaseAnim()
        {
            _btlUi.SetTurnType( TurnType.ENEMY_TURN );

            base.StartPhaseAnim();
        }
    }
}