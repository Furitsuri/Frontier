using Frontier.StateMachine;

namespace Frontier.Battle
{
    public class PlPhaseStateAnimation : PhaseAnimationStateBase
    {
        protected override void StartPhaseAnim()
        {
            PhaseAnimPresenter.SetTurnType( TurnType.PLAYER_TURN );

            base.StartPhaseAnim();
        }
    }
}