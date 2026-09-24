using Frontier.StateMachine;
using Frontier.UI;
using Zenject;
using static Constants;

namespace Frontier.Shop
{
    /// <summary>
    /// 購入する個数を選ぶステート(ShopBrowseStateの子)。複数個購入できる商品でのみ挟まれる。
    /// 商品一覧で選択中の商品の右隣に個数選択パネルを出し、上下で個数を増減、決定で購入確認(ShopPurchaseConfirmState)へ進む。
    /// キャンセルなら何も購入せず商品一覧へ戻る。表示中は、店主の「いくつ御購入されますか？」を画面右下に出す。
    /// 購入確認で購入が済んだ場合は、この画面へ戻らずそのまま商品一覧まで戻る。
    /// 戻った後の店主の言葉は、ShopBrowseState側が切り替える。
    /// </summary>
    public sealed class ShopQuantityState : PhaseStateBase
    {
        private enum ShopQuantityTransitTag
        {
            PURCHASE_CONFIRM = 0,
        }

        [Inject] private TalkWindowPresenter _talkWindowPresenter = null;

        private ShopPresenter _presenter = null;

        // 購入確認で購入が済んだか。済んでいる場合、購入確認から戻った時点でこの画面も終了する
        private bool _isPurchased = false;

        public override void AssignPresenter( PhasePresenterBase presenter )
        {
            _presenter = presenter as ShopPresenter;
        }

        public override void Init( object context )
        {
            base.Init( context );

            _isPurchased = false;

            _presenter.BeginQuantitySelection();

            ShowQuestion();
        }

        /// <summary>
        /// 購入確認から戻ってきた際、購入が済んでいれば商品一覧まで戻ります。
        /// 「いいえ」等で購入しなかった場合は、個数を選び直せるよう店主の問いかけを再表示します。
        /// </summary>
        public override void RestartState()
        {
            base.RestartState();

            if( _isPurchased )
            {
                // 個数選択の表示(個数パネル・購入予定額)は、購入確認のお礼の間も残しておいたため、ここで止める。
                // Back()による終了(ExitState)を待つと、購入確認が終わってからそこまでの間に、購入予定額が
                // 一瞬だけヘッダーに再表示されてしまう
                _presenter.EndQuantitySelection();

                Back();

                return;
            }

            ShowQuestion();
        }

        public override object ExitState()
        {
            _presenter.EndQuantitySelection();

            return base.ExitState();
        }

        public override void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            _inputFcd.RegisterInputCodes(
               (GuideIcon.VERTICAL_CURSOR, "QUANTITY", CanAcceptDefault, new AcceptContextInput( AcceptDirection ), MENU_DIRECTION_INPUT_INTERVAL, hashCode),
               (GuideIcon.CONFIRM,         "CONFIRM",  CanAcceptDefault, new AcceptContextInput( AcceptConfirm ), 0.0f, hashCode),
               (GuideIcon.CANCEL,          "BACK",     CanAcceptDefault, new AcceptContextInput( AcceptCancel ), 0.0f, hashCode)
            );
        }

        protected override bool AcceptDirection( InputContext context )
        {
            return _presenter.MoveQuantity( context.Cursor );
        }

        protected override bool AcceptConfirm( InputContext context )
        {
            if( !base.AcceptConfirm( context ) ) { return false; }

            // 選んだ個数を購入確認へ渡す。購入が済んだ際に、この画面も終了できるよう完了通知も渡す
            // (Back()時にStateの戻り値は破棄されるため、子から親へはコールバックで伝える)
            SetSendTransitionContext( new ShopPurchaseConfirmContext( _presenter.SelectedQuantity, OnPurchased ) );
            TransitState( ( int ) ShopQuantityTransitTag.PURCHASE_CONFIRM );

            return true;
        }

        /// <summary>
        /// 購入確認で購入が済んだ際の通知。購入確認から戻った時点でこの画面も終了するためのフラグを立てる。
        /// 個数選択の表示は、購入後のお礼の間も(購入確認の表示と同様に)残す
        /// </summary>
        private void OnPurchased()
        {
            _isPurchased = true;
        }

        protected override bool AcceptCancel( InputContext context )
        {
            if( !base.AcceptCancel( context ) ) { return false; }

            Back();

            return true;
        }

        private void ShowQuestion()
        {
            _talkWindowPresenter.ShowBottomRight( LocKey.UI_TALK_SHOPKEEPER_NAME, LocKey.UI_TALK_SHOP_ASK_QUANTITY );
        }
    }
}
