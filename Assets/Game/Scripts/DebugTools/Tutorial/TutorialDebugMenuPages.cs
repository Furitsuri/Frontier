using Frontier.DebugTools.DebugMenu;

#if UNITY_EDITOR

namespace Frontier.DebugTools.Tutorial
{
    /// <summary>
    /// デバッグメニューの、チュートリアル(TUTORIAL)カテゴリのページです
    /// </summary>
    public static class TutorialDebugMenuPages
    {
        // チュートリアルの項目。番号を指定したチュートリアルの閲覧(表示済みの記録は残さない)と、既読データの編集を行う
        public static readonly DebugMenuPage ENTRIES = new DebugMenuPage( DebugMenuCategory.TUTORIAL, "ENTRIES" );
    }
}

#endif // UNITY_EDITOR
