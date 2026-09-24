using Frontier.StateMachine;
using Frontier.UI;
using Zenject;
using static Constants;

namespace Frontier.Shop
{
    /// <summary>
    /// ショップの商品一覧画面のステート(ShopPhaseHandlerのルート)。
    /// 上下でカーソル移動、決定で選択中の商品を購入、キャンセルで退店確認(ShopLeaveConfirmState)へ遷移する。
    /// 購入できない商品(在庫切れ・アニマ不足)にカーソルがある間は、決定の入力ガイドを無効表示にする。
    /// 表示中は、画面右下に店主の挨拶(会話ウィンドウ)を出す。退店確認から戻ってきた際も再表示する。
    /// </summary>
    public sealed class ShopBrowseState : PhaseStateBase
    {
        private enum ShopBrowseTransitTag
        {
            LEAVE_CONFIRM = 0,
        }

        [Inject] private ShopHandler _shopHandler                 = null;
        [Inject] private TalkWindowPresenter _talkWindowPresenter = null;

        private ShopPresenter _presenter = null;

        public override void AssignPresenter( PhasePresenterBase presenter )
        {
            _presenter = presenter as ShopPresenter;
        }

        public override void Init( object context )
        {
            base.Init( context );

            ShowGreeting();
        }

        /// <summary>
        /// 退店確認(ShopLeaveConfirmState)で「いいえ」を選んで戻ってきた際、隠れた挨拶を再表示します
        /// </summary>
        public override void RestartState()
        {
            base.RestartState();

            ShowGreeting();
        }

        public override object ExitState()
        {
            _talkWindowPresenter.Hide();

            return base.ExitState();
        }

        public override void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            _inputFcd.RegisterInputCodes(
               (GuideIcon.VERTICAL_CURSOR, "SELECT", CanAcceptDefault,  new AcceptContextInput( AcceptDirection ), MENU_DIRECTION_INPUT_INTERVAL, hashCode),
               (GuideIcon.CONFIRM,         "BUY",    CanAcceptPurchase, new AcceptContextInput( AcceptConfirm ), 0.0f, hashCode),
               (GuideIcon.CANCEL,          "LEAVE",  CanAcceptDefault,  new AcceptContextInput( AcceptCancel ), 0.0f, hashCode)
            );
        }

        protected override bool AcceptDirection( InputContext context )
        {
            return _presenter.MoveSelection( context.Cursor );
        }

        protected override bool AcceptConfirm( InputContext context )
        {
            if( !base.AcceptConfirm( context ) ) { return false; }
            if( !_presenter.TryGetSelectedItem( out var item ) ) { return false; }
            if( _shopHandler.Purchase( item ) != PurchaseResult.Success ) { return false; }

            _presenter.Refresh();

            return true;
        }

        protected override bool AcceptCancel( InputContext context )
        {
            if( !base.AcceptCancel( context ) ) { return false; }

            TransitState( ( int ) ShopBrowseTransitTag.LEAVE_CONFIRM );

            return true;
        }

        private void ShowGreeting()
        {
            _talkWindowPresenter.ShowBottomRight( LocKey.UI_TALK_SHOPKEEPER_NAME, LocKey.UI_TALK_SHOP_GREETING );
        }

        private bool CanAcceptPurchase()
        {
            return CanAcceptDefault()
                && _presenter.TryGetSelectedItem( out var item )
                && _shopHandler.CanPurchase( item );
        }
    }
}
