using Frontier.StateMachine;
using Frontier.UI;
using Zenject;

namespace Frontier.Shop
{
    /// <summary>
    /// 退店してよいかを店主が確認するステート(ShopBrowseStateの子)。会話ウィンドウ形式のYes/No確認。
    /// 「いいえ」なら商品一覧へ戻る。「はい」なら挨拶(TalkWindowCushionState)を挟んでから、
    /// ショップ全体(フェーズ)を終了する。
    /// </summary>
    public sealed class ShopLeaveConfirmState : ConfirmPhaseStateBase
    {
        private enum ShopLeaveConfirmTransitTag
        {
            FAREWELL = 0,
        }

        [Inject] private TalkWindowConfirmPresenter _talkConfirmPresenter = null;

        private bool _isFarewellSpoken = false;

        public override void Init( object context )
        {
            base.Init( context );

            _isFarewellSpoken = false;

            _talkConfirmPresenter.SetConfirmMessage( LocKey.UI_TALK_SHOPKEEPER_NAME, LocKey.UI_TALK_SHOP_LEAVE_CONFIRM );
        }

        /// <summary>
        /// 退店確認は会話ウィンドウ形式(TalkWindowConfirmPresenter)を使うため、
        /// ツリー全体に伝播されるShopPresenterではなく専用のPresenterを割り当てる。
        /// </summary>
        public override void AssignPresenter( PhasePresenterBase presenter )
        {
            _confirmPresenter = _talkConfirmPresenter;
        }

        /// <summary>
        /// 挨拶(TalkWindowCushionState)から戻ってきた場合は、ショップ全体を終了する
        /// </summary>
        public override void RestartState()
        {
            base.RestartState();

            if( _isFarewellSpoken )
            {
                _isEndedPhase = true;
                Back();
            }
        }

        protected override bool AcceptConfirm( InputContext context )
        {
            if( !base.AcceptConfirm( context ) ) { return false; }

            if( _commandList.GetCurrentValue() != ( int ) ConfirmTag.YES )
            {
                Back();

                return true;
            }

            // 挨拶への遷移はExitStateではなくPauseStateしか呼ばれないため、確認ダイアログは先に隠す
            _confirmPresenter.SetActiveConfirmUI( false, UIType );

            _isFarewellSpoken = true;
            SetSendTransitionContext( new TalkWindowCushionContext( LocKey.UI_TALK_SHOPKEEPER_NAME, LocKey.UI_TALK_SHOP_FAREWELL ) );
            TransitState( ( int ) ShopLeaveConfirmTransitTag.FAREWELL );

            return true;
        }
    }
}
