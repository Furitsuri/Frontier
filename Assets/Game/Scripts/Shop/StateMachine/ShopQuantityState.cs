using Frontier.StateMachine;
using Frontier.UI;
using Zenject;
using static Constants;

namespace Frontier.Shop
{
    /// <summary>
    /// 購入する個数を選ぶステート(ShopBrowseStateの子)。
    /// 商品一覧で選択中の商品の右隣に個数選択パネルを出し、上下で個数を増減、決定でその個数を購入して商品一覧へ戻る。
    /// キャンセルなら何も購入せず商品一覧へ戻る。表示中は、店主の「いくつ御購入されますか？」を画面右下に出す。
    /// 戻った後の店主の言葉は、ShopBrowseState側が切り替える。
    /// </summary>
    public sealed class ShopQuantityState : PhaseStateBase
    {
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

            _presenter.BeginQuantitySelection();

            _talkWindowPresenter.ShowBottomRight( LocKey.UI_TALK_SHOPKEEPER_NAME, LocKey.UI_TALK_SHOP_ASK_QUANTITY );
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
               (GuideIcon.CONFIRM,         "BUY",      CanAcceptDefault, new AcceptContextInput( AcceptConfirm ), 0.0f, hashCode),
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
            if( !_presenter.TryGetSelectedItem( out var item ) ) { return false; }
            if( _shopHandler.Purchase( item, _presenter.SelectedQuantity ) != PurchaseResult.Success ) { return false; }

            // 購入でアニマは減算済みのため、Back()による遷移(ExitState)を待たず予定額の表示を止める
            // (待つと、減算後のアニマ数値と予定額が同時に表示される瞬間が生じる)
            _presenter.EndQuantitySelection();
            _presenter.Refresh();

            Back();

            return true;
        }

        protected override bool AcceptCancel( InputContext context )
        {
            if( !base.AcceptCancel( context ) ) { return false; }

            Back();

            return true;
        }
    }
}
