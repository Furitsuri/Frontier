#if UNITY_EDITOR

namespace Frontier.DebugTools.DebugMenu
{
    /// <summary>
    /// デバッグメニューのページです。いずれかのカテゴリに属し、項目の一覧を持ちます。
    /// ページはカテゴリ毎に用途が異なるため、各カテゴリの側で static readonly のインスタンスとして定義し、参照の同一性で区別します
    /// (戦闘用のページは BattleDebugMenuPages を参照)
    /// </summary>
    public sealed class DebugMenuPage
    {
        public DebugMenuCategory Category { get; }
        public string Name { get; }

        public DebugMenuPage( DebugMenuCategory category, string name )
        {
            Category    = category;
            Name        = name;
        }
    }
}

#endif // UNITY_EDITOR
