using System;

namespace Frontier.Shop
{
    /// <summary>
    /// ShopPurchaseConfirmState に渡すコンテキストです。購入する個数と、購入が済んだ際の完了通知を保持します。
    /// 完了通知は、購入確認から戻った後に遷移元の画面も終了させたい場合(個数選択)に使います。
    /// Back()時にStateの戻り値は破棄されるため、子から親へはこのコールバックで伝えます。
    /// </summary>
    public class ShopPurchaseConfirmContext
    {
        /// <summary>購入する個数(個数選択を挟まない単数の購入では1)</summary>
        public int Quantity { get; }

        /// <summary>購入が済んだ際に呼ばれる通知(不要な場合はnull)</summary>
        public Action OnPurchased { get; }

        public ShopPurchaseConfirmContext( int quantity, Action onPurchased )
        {
            Quantity    = quantity;
            OnPurchased = onPurchased;
        }
    }
}
