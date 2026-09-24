using Frontier.StateMachine;
using Frontier.UI;
using Zenject;
using static Constants;

namespace Frontier.Shop
{
    /// <summary>
    /// ショップの商品一覧画面のステート(ShopPhaseHandlerのルート)。
    /// 上下でカーソル移動、決定で選択中の商品を購入、キャンセルで退店確認(ShopLeaveConfirmState)へ遷移する。
    /// 決定した商品を複数個購入できる場合は個数選択(ShopQuantityState)へ、1個しか購入できない場合は
    /// 直接購入確認(ShopPurchaseConfirmState)へ進む(購入は必ず購入確認を通る)。
    /// 購入できない商品(在庫切れ・アニマ不足)にカーソルがある間は、決定の入力ガイドを無効表示にする。
    /// 表示中は、画面右下に店主の言葉(会話ウィンドウ)を出す。入店直後は挨拶、他のステートへ一度でも遷移して
    /// 戻ってきた後は「他に御用はございますか？」に切り替える。
    /// </summary>
    public sealed class ShopBrowseState : PhaseStateBase
    {
        private enum ShopBrowseTransitTag
        {
            LEAVE_CONFIRM = 0,
            QUANTITY,
            PURCHASE_CONFIRM,
        }

        [Inject] private ShopHandler _shopHandler                 = null;
        [Inject] private TalkWindowPresenter _talkWindowPresenter = null;

        private ShopPresenter _presenter = null;

        // 入店してから、この商品一覧から別のステートへ遷移したことがあるか(店主の言葉の出し分けに使う)。
        // オプションメニュー等による中断→再開ではRestartState()が呼ばれるが、遷移ではないため対象外。
        private bool _hasLeftBrowse = false;

        public override void AssignPresenter( PhasePresenterBase presenter )
        {
            _presenter = presenter as ShopPresenter;
        }

        public override void Init( object context )
        {
            base.Init( context );

            _hasLeftBrowse = false;

            ShowShopkeeperMessage();
        }

        /// <summary>
        /// 退店確認や個数選択から戻ってきた際、隠れた店主の言葉を再表示します
        /// </summary>
        public override void RestartState()
        {
            base.RestartState();

            ShowShopkeeperMessage();
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

            int maxQuantity = _shopHandler.GetMaxPurchasableQuantity( item );
            if( maxQuantity < 1 ) { return false; }

            // 複数個購入できる場合は、まず個数選択を挟む
            if( 1 < maxQuantity )
            {
                TransitToChild( ShopBrowseTransitTag.QUANTITY );

                return true;
            }

            // 1個しか購入できない場合は個数選択を省略し、直接購入確認へ進む(購入は必ず購入確認を通る)
            SetSendTransitionContext( new ShopPurchaseConfirmContext( 1, null ) );
            TransitToChild( ShopBrowseTransitTag.PURCHASE_CONFIRM );

            return true;
        }

        protected override bool AcceptCancel( InputContext context )
        {
            if( !base.AcceptCancel( context ) ) { return false; }

            TransitToChild( ShopBrowseTransitTag.LEAVE_CONFIRM );

            return true;
        }

        private void TransitToChild( ShopBrowseTransitTag tag )
        {
            _hasLeftBrowse = true;

            TransitState( ( int ) tag );
        }

        private void ShowShopkeeperMessage()
        {
            var messageKey = _hasLeftBrowse ? LocKey.UI_TALK_SHOP_ANYTHING_ELSE : LocKey.UI_TALK_SHOP_GREETING;

            _talkWindowPresenter.ShowBottomRight( LocKey.UI_TALK_SHOPKEEPER_NAME, messageKey );
        }

        private bool CanAcceptPurchase()
        {
            return CanAcceptDefault()
                && _presenter.TryGetSelectedItem( out var item )
                && _shopHandler.CanPurchase( item );
        }
    }
}
