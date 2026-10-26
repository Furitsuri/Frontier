using Frontier.Battle;
using Frontier.StateMachine;

namespace Frontier.UI
{
    /// <summary>
    /// フェーズ開始時の「PLAYER TURN」「ENEMY TURN」等の演出を仲介するPresenterです。
    /// BattleRoutinePresenterが子Presenterとして保持し、PhaseAnimationStateBaseから利用されます。
    /// </summary>
    public class PhaseAnimationPresenter : PhasePresenterBase
    {
        /// <summary>
        /// 演出に表示するターンの種類を設定します
        /// </summary>
        public void SetTurnType( TurnType turnType )
        {
            _uiSystem.BattleUi.SetTurnType( turnType );
        }

        /// <summary>
        /// フェーズ開始演出のアニメーションを開始します
        /// </summary>
        public void StartPhaseAnim()
        {
            _uiSystem.BattleUi.StartAnimPhaseUI();
        }

        /// <summary>
        /// フェーズ開始演出のアニメーションが再生中かどうかを取得します
        /// </summary>
        public bool IsPlayingPhaseAnim()
        {
            return _uiSystem.BattleUi.IsPlayingPhaseUI();
        }
    }
}
