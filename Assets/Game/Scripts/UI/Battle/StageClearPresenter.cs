using Frontier.StateMachine;

namespace Frontier.UI
{
    /// <summary>
    /// ステージクリア時の「STAGE CLEAR」演出とリザルト画面の表示を仲介するPresenterです。
    /// BattleRoutinePresenterが子Presenterとして保持し、StageClearStateから利用されます。
    /// </summary>
    public class StageClearPresenter : PhasePresenterBase
    {
        /// <summary>
        /// 「STAGE CLEAR」演出のUIを表示し、アニメーションを開始します
        /// </summary>
        public void StartStageClearAnim()
        {
            _uiSystem.BattleUi.ToggleStageClearUI( true );
            _uiSystem.BattleUi.StartStageClearAnim();
        }

        /// <summary>
        /// 「STAGE CLEAR」演出のアニメーションが再生中かどうかを取得します
        /// </summary>
        public bool IsPlayingStageClearAnim()
        {
            return _uiSystem.BattleUi.StageClear.IsPlayingAnim();
        }

        /// <summary>
        /// リザルト画面を表示します
        /// </summary>
        public void ShowStageResult( int anima, int turnCount )
        {
            _uiSystem.BattleUi.ShowStageResult( anima, turnCount );
        }

        /// <summary>
        /// リザルト画面を非表示にします
        /// </summary>
        public void HideStageResult()
        {
            _uiSystem.BattleUi.HideStageResult();
        }
    }
}
