using Frontier.StateMachine;

namespace Frontier.Battle
{
    public class EmPhaseStateAnimation : PhaseAnimationStateBase
    {
        protected override void StartPhaseAnim()
        {
            PhaseAnimPresenter.SetTurnType( TurnType.ENEMY_TURN );

            base.StartPhaseAnim();
        }
    }
}