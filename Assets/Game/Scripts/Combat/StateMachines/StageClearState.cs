using System;
using Frontier.StateMachine;

namespace Frontier.Battle
{
    /// <summary>
    /// ステージクリア判定後、アニマ獲得エフェクトの再生完了・「STAGE CLEAR」演出の開始から、
    /// 演出の終了・CONFIRM入力を経てリザルト画面(獲得アニマ)を表示するまでの一連の流れを担う専用ステートです。
    /// 通常のフェーズ(Deployment/Player/Enemy/Other)の循環には含めず、BattleRoutineControllerが
    /// ステージクリア判定時にのみ、Treeへ登録せず単体で駆動します。
    /// </summary>
    public class StageClearState : PhaseStateBase
    {
        private enum Phase
        {
            NONE,               // Begin()未呼び出し(通常の戦闘フェーズ中)
            WAIT_ANIMA_REWARD,  // アニマ獲得エフェクト(撃破報酬の球)の再生完了待ち
            WAIT_INTERVAL,      // アニマ獲得エフェクト終了後、「STAGE CLEAR」演出開始までの間隔待ち
            WAIT_ANIM,          // 「STAGE CLEAR」演出の終了待ち
            WAIT_CONFIRM,       // CONFIRM入力待ち
            DONE,               // リザルト画面表示済み
        }

        private Phase _phase;
        // Begin()時点ではまだ最後の撃破報酬(アニマ獲得エフェクト)が到達しておらず加算前の値である
        // 可能性があるため、値そのものではなく取得用のFuncを保持し、実際に表示する直前(AcceptConfirm時)に読み出す。
        private Func<int> _getBattleAnima;
        private int _turnCount;
        private float _intervalElapsed;

        /// <summary>
        /// Begin()が呼ばれ、ステージクリア演出のシーケンスが進行中かどうかを取得します。
        /// BattleRoutineControllerはこの値を見て、通常の戦闘フェーズ更新を行うかどうかを判断します。
        /// </summary>
        public bool IsActive => _phase != Phase.NONE;

        /// <summary>
        /// リザルト画面表示まで完了したかどうかを取得します
        /// </summary>
        public bool IsDone => _phase == Phase.DONE;

        /// <summary>
        /// このステートを開始します。撃破位置から放出中のアニマ獲得エフェクトが残っている場合は、
        /// その再生が終わるまで「STAGE CLEAR」演出の開始(戦闘中HUDの非表示を含む)を待ちます。
        /// </summary>
        /// <param name="getBattleAnima">リザルト画面表示直前に呼び出す、現在の戦闘中アニマ取得用の関数</param>
        /// <param name="turnCount">リザルト画面に表示する、クリアまでにかかったターン数</param>
        public void Begin( Func<int> getBattleAnima, int turnCount )
        {
            _getBattleAnima = getBattleAnima;
            _turnCount      = turnCount;
            _phase          = Phase.WAIT_ANIMA_REWARD;

            OnEnter( null );
        }

        /// <summary>
        /// 毎フレーム呼び出してください。アニマ獲得エフェクトの再生完了(数値への反映)を待ち、
        /// 一定時間の間隔を空けてから「STAGE CLEAR」演出を開始し、その終了を監視して
        /// 終了次第CONFIRM入力の受付へ移行します。
        /// </summary>
        public override bool Update()
        {
            switch( _phase )
            {
                case Phase.WAIT_ANIMA_REWARD:
                    if( !_uiSystem.BattleUi.IsAnyAnimaRewardEffectPlaying() )
                    {
                        _intervalElapsed = 0f;
                        _phase = Phase.WAIT_INTERVAL;
                    }
                    break;

                case Phase.WAIT_INTERVAL:
                    _intervalElapsed += DeltaTimeProvider.DeltaTime;
                    if( Constants.STAGE_CLEAR_ANIMA_INTERVAL <= _intervalElapsed )
                    {
                        _uiSystem.BattleUi.ToggleStageClearUI( true );
                        _uiSystem.BattleUi.StartStageClearAnim();
                        _phase = Phase.WAIT_ANIM;
                    }
                    break;

                case Phase.WAIT_ANIM:
                    if( !_uiSystem.BattleUi.StageClear.IsPlayingAnim() )
                    {
                        _phase = Phase.WAIT_CONFIRM;
                    }
                    break;
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

            _uiSystem.BattleUi.ShowStageResult( _getBattleAnima(), _turnCount );
            _phase = Phase.DONE;
            UnregisterInputCodes( GetInputCodeHash() );

            return true;
        }
    }
}
