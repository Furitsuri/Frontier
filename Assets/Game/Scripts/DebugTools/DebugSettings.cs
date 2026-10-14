#if UNITY_EDITOR

namespace Frontier.DebugTools
{
    /// <summary>
    /// デバッグ表示等のON/OFFをまとめて保持する設定です。
    /// 値の変更はデバッグメニューの項目(またはコード)から行い、表示側はこの値を参照するだけにします
    /// </summary>
    public class DebugSettings
    {
        public bool ShowThinkingTypeLabel = false;  // NPCの頭上に思考タイプ(INTERCEPT型の場合は待ち構え方・集団ID・起動状態も)を表示する
    }
}

#endif // UNITY_EDITOR
