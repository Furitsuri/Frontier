using UnityEngine;

namespace Frontier
{
    /// <summary>
    /// ゲームを終了する処理を提供します。
    /// フィールドメニューに限らず、複数のシーン・ステートから共通して呼び出される可能性があるため、
    /// 特定のシーン・クラスに紐づかない独立したクラスとしています。
    /// </summary>
    static public class GameQuitService
    {
        /// <summary>
        /// ゲームを終了します。
        /// Unity Editor実行中はPlayModeを終了し、ビルド後のアプリケーションではApplication.Quit()を呼びます。
        /// </summary>
        static public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
