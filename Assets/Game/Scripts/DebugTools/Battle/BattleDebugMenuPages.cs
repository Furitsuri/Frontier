using Frontier.DebugTools.DebugMenu;

#if UNITY_EDITOR

namespace Frontier.DebugTools.Battle
{
    /// <summary>
    /// デバッグメニューの、戦闘(BATTLE)カテゴリのページです
    /// </summary>
    public static class BattleDebugMenuPages
    {
        // 表示。戦闘中の表示のON/OFFなど
        public static readonly DebugMenuPage DISPLAY            = new DebugMenuPage( DebugMenuCategory.BATTLE, "DISPLAY" );
        // 選択中キャラクター。ページを開いた時点でグリッドカーソル上にいたキャラクターへの操作
        public static readonly DebugMenuPage SELECTED_CHARACTER = new DebugMenuPage( DebugMenuCategory.BATTLE, "SELECTED CHARACTER" );
    }
}

#endif // UNITY_EDITOR
