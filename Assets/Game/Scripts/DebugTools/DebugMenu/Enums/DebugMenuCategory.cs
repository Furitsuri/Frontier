namespace Frontier.DebugTools.DebugMenu
{
    /// <summary>
    /// デバッグメニューの最上位の区分けです。
    /// 各カテゴリの中に1つ以上のページ(DebugMenuPage)があり、ページの中に項目が並びます
    /// </summary>
    public enum DebugMenuCategory
    {
        BATTLE = 0,     // 戦闘。戦闘の実行中に、動作中のものへ直接干渉するための項目
        TUTORIAL,       // チュートリアル

        NUM,
    }
}
