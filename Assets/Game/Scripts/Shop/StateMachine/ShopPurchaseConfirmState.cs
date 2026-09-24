using Frontier.StateMachine;
using Frontier.UI;
using System;
using Zenject;

namespace Frontier.Shop
{
    /// <summary>
    /// 購入の最終確認を行うステート。商品が単数・複数に関わらず、購入は必ずこのステートを通る
    /// (商品一覧の子、および個数選択の子として、同一インスタンスが使われる)。
    /// 商品一覧などの奥の表示はぼかし+灰色の覆いで隠し(ScreenBlurOverlayPresenter)、その手前の画面中央に
    /// 購入する商品名・個数・合計金額を出す。ヘッダーのアニマ増減差分は大きく強調して表示する。
    /// 店主の「こちらを御購入されますか？」は右下の会話ウィンドウで、そのままYes/Noを選択する。
    /// 「はい」で購入して呼び出し元へ戻る。「いいえ」・キャンセルでは何も購入せず呼び出し元(個数選択、または商品一覧)へ戻る。
    /// </summary>
    public sealed class ShopPurchaseConfirmState : ConfirmPhaseStateBase
    {
        [Inject] private ShopHandler _shopHandler                         = null;
        [Inject] private TalkWindowConfirmPresenter _talkConfirmPresenter = null;
        [Inject] private ScreenBlurOverlayPresenter _blurOverlayPresenter = null;

        private ShopPresenter _shopPresenter = null;
        private int _quantity                = 1;
        private Action _onPurchased          = null;

        /// <summary>
        /// 確認は会話ウィンドウ形式(TalkWindowConfirmPresenter)を使うため、確認用のPresenterは専用のものを割り当て、
        /// ツリー全体に伝播されるShopPresenterは購入内容の表示・購入対象の取得用として別に保持する。
        /// </summary>
        public override void AssignPresenter( PhasePresenterBase presenter )
        {
            _shopPresenter    = presenter as ShopPresenter;
            _confirmPresenter = _talkConfirmPresenter;
        }

        public override void Init( object context )
        {
            base.Init( context );

            ShopPurchaseConfirmContext purchaseContext = null;
            ReceiveContext( ref purchaseContext, context );
            NullCheck.AssertNotNull( purchaseContext, nameof( purchaseContext ) );

            _quantity    = purchaseContext.Quantity;
            _onPurchased = purchaseContext.OnPurchased;

            // 確認の既定は「はい」(購入する)。誤操作を防ぐ確認というより、購入内容を見せる最終画面のため
            _commandList.SetCurrentValue( ( int ) ConfirmTag.YES );

            _talkConfirmPresenter.SetConfirmMessage( LocKey.UI_TALK_SHOPKEEPER_NAME, LocKey.UI_TALK_SHOP_PURCHASE_CONFIRM );

            _blurOverlayPresenter.Show();
            _shopPresenter.BeginPurchaseConfirm( _quantity );
        }

        public override object ExitState()
        {
            EndDisplay();

            return base.ExitState();
        }

        protected override bool AcceptConfirm( InputContext context )
        {
            if( !base.AcceptConfirm( context ) ) { return false; }

            // 「いいえ」なら何も購入せず戻る
            if( _commandList.GetCurrentValue() != ( int ) ConfirmTag.YES )
            {
                Back();

                return true;
            }

            if( _shopPresenter.TryGetSelectedItem( out var item ) && _shopHandler.Purchase( item, _quantity ) == PurchaseResult.Success )
            {
                // 購入でアニマは減算済みのため、Back()による遷移(ExitState)を待たず購入予定額の表示を止める
                // (待つと、減算後のアニマ数値と購入予定額が同時に表示される瞬間が生じる)
                EndDisplay();
                _shopPresenter.Refresh();

                _onPurchased?.Invoke();
            }

            Back();

            return true;
        }

        /// <summary>
        /// 購入確認のために出していた表示(覆い・購入内容・強調表示)を止める。複数回呼ばれても問題ない
        /// </summary>
        private void EndDisplay()
        {
            _shopPresenter.EndPurchaseConfirm();
            _blurOverlayPresenter.Hide();
        }
    }
}
