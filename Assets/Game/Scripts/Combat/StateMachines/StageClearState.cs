using Frontier.StateMachine;

namespace Frontier.Battle
{
    /// <summary>
    /// ステージクリア判定後、「STAGE CLEAR」演出の開始から、演出の終了・CONFIRM入力を経て
    /// リザルト画面(獲得アニマ)を表示するまでの一連の流れを担う専用ステートです。
    /// 通常のフェーズ(Deployment/Player/Enemy/Other)の循環には含めず、BattleRoutineControllerが
    /// ステージクリア判定時にのみ、Treeへ登録せず単体で駆動します。
    /// </summary>
    public class StageClearState : PhaseStateBase
    {
        private enum Phase
        {
            WAIT_ANIM,      // 「STAGE CLEAR」演出の終了待ち
            WAIT_CONFIRM,   // CONFIRM入力待ち
            DONE,           // リザルト画面表示済み
        }

        private Phase _phase;
        private int _battleAnima;

        /// <summary>
        /// リザルト画面表示まで完了したかどうかを取得します
        /// </summary>
        public bool IsDone => _phase == Phase.DONE;

        /// <summary>
        /// このステートを開始します。「STAGE CLEAR」演出の開始・戦闘中HUD(アニマ表示/
        /// パラメータウィンドウ)の非表示もここで行います。
        /// </summary>
        /// <param name="battleAnima">リザルト画面に表示する、今回の戦闘で獲得したアニマ量</param>
        public void Begin( int battleAnima )
        {
            _battleAnima = battleAnima;
            _phase       = Phase.WAIT_ANIM;

            _uiSystem.BattleUi.ToggleStageClearUI( true );
            _uiSystem.BattleUi.StartStageClearAnim();

            OnEnter( null );
        }

        /// <summary>
        /// 毎フレーム呼び出してください。「STAGE CLEAR」演出の終了を監視し、
        /// 終了次第CONFIRM入力の受付へ移行します。
        /// </summary>
        public override bool Update()
        {
            if( _phase == Phase.WAIT_ANIM && !_uiSystem.BattleUi.StageClear.IsPlayingAnim() )
            {
                _phase = Phase.WAIT_CONFIRM;
            }

            return false;
        }

        public override void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            _inputFcd.RegisterInputCodes(
                ( GuideIcon.CONFIRM, "CONFIRM", CanAcceptConfirm, new AcceptContextInput( AcceptConfirm ), 0.0f, hashCode )
            );
        }

        protected override bool CanAcceptConfirm()
        {
            return _phase == Phase.WAIT_CONFIRM;
        }

        protected override bool AcceptConfirm( InputContext context )
        {
            if( !AcceptConfirmCore( context ) ) { return false; }

            _uiSystem.BattleUi.ShowStageResult( _battleAnima );
            _phase = Phase.DONE;
            UnregisterInputCodes( GetInputCodeHash() );

            return true;
        }
    }
}
