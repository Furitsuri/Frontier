using Frontier.UI;
using Zenject;

namespace Frontier.StateMachine
{
    /// <summary>
    /// 会話ウィンドウ(画面右上)を表示するクッション画面。特定のフェーズ・Sceneに依存しない
    /// 汎用Stateのため、話者名・メッセージのLocKeyは遷移元から<see cref="TalkWindowCushionContext"/>
    /// 経由で受け取り、任意の親Stateの子として使い回せる(例: RecruitScene「雇用」「解雇」で
    /// 雇用/解雇不可の場合、今後追加予定のShopSceneでの店主の会話等)。
    /// Confirm/Cancelいずれの入力でも会話ウィンドウを閉じBack()するのみで、カーソル・
    /// 選択中キャラクターのパラメータパネル等の再表示は親側のRestartState()で行う
    /// (Back()時にStateBase.ExitState()の戻り値は破棄される仕様のため、親からcontext経由で
    /// 情報を受け取ることはできない)。
    /// </summary>
    public sealed class TalkWindowCushionState : PhaseStateBase
    {
        [Inject] private TalkWindowPresenter _talkWindowPresenter = null;

        public override void Init( object context )
        {
            base.Init( context );

            TalkWindowCushionContext talkContext = null;
            ReceiveContext( ref talkContext, context );
            NullCheck.AssertNotNull( talkContext, nameof( talkContext ) );

            _talkWindowPresenter.Show( talkContext.SpeakerKey, talkContext.MessageKey );
        }

        public override void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            _inputFcd.RegisterInputCodes(
               (GuideIcon.CONFIRM, "CONTINUE", CanAcceptDefault, new AcceptContextInput( AcceptConfirm ), 0.0f, hashCode),
               (GuideIcon.CANCEL,  "CONTINUE", CanAcceptDefault, new AcceptContextInput( AcceptCancel ), 0.0f, hashCode)
            );
        }

        protected override bool AcceptConfirm( InputContext context )
        {
            if( !base.AcceptConfirm( context ) ) { return false; }

            DismissGreeting();

            return true;
        }

        protected override bool AcceptCancel( InputContext context )
        {
            if( !base.AcceptCancel( context ) ) { return false; }

            DismissGreeting();

            return true;
        }

        private void DismissGreeting()
        {
            _talkWindowPresenter.Hide();

            Back();
        }
    }
}
