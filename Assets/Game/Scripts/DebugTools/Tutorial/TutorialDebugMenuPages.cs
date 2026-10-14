using Frontier.DebugTools.DebugMenu;

#if UNITY_EDITOR

namespace Frontier.DebugTools.Tutorial
{
    /// <summary>
    /// デバッグメニューの、チュートリアル(TUTORIAL)カテゴリのページです
    /// </summary>
    public static class TutorialDebugMenuPages
    {
        // チュートリアルの閲覧。番号を指定したチュートリアルを、表示済みの記録を残さずに表示する
        public static readonly DebugMenuPage VIEWER = new DebugMenuPage( DebugMenuCategory.TUTORIAL, "VIEWER" );
    }
}

#endif // UNITY_EDITOR
