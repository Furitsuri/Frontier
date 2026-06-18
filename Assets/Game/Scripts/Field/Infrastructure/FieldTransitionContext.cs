namespace Frontier.Field
{
    /// <summary>
    /// フィールドシーンと他シーン間でデータを受け渡す静的コンテキスト。
    /// SceneManager.LoadScene をまたぐため DI が使えないので static で保持する。
    /// </summary>
    public static class FieldTransitionContext
    {
        /// <summary>フィールドから遷移した場合 true。GameMain 側でフィールド帰還ルートを判断するために使用。</summary>
        public static bool IsFromField { get; private set; } = false;

        /// <summary>遷移先で読み込むステージインデックス。FilePathRegistry.StageNames[] のインデックス。</summary>
        public static int StageIndex { get; private set; } = 0;

        /// <summary>クリアしたノードのId。GameMain 終了後に FieldScene が読み取る。</summary>
        public static int ClearedNodeId { get; private set; } = -1;

        /// <summary>フィールドから戦闘シーンへ遷移する前に呼ぶ。</summary>
        public static void SetupBattleTransition( int stageIndex, int nodeId )
        {
            IsFromField   = true;
            StageIndex    = stageIndex;
            ClearedNodeId = nodeId;
        }

        /// <summary>フィールドへ帰還したとき、または直接起動したときに呼んでリセットする。</summary>
        public static void Clear()
        {
            IsFromField   = false;
            StageIndex    = 0;
            ClearedNodeId = -1;
        }
    }
}
