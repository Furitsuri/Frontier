namespace Frontier.Shop
{
    /// <summary>
    /// ショップUIの背景表示モードです。呼び出し元(戦闘中の商人NPC/Fieldショップノード)に応じてShopPresenterへ渡します。
    /// </summary>
    public enum ShopBackgroundMode
    {
        Battle,
        Field,
    }
}
