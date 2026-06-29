using UnityEngine;

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

        /// <summary>
        /// 戦闘シーンへの遷移を開始した時刻（Time.realtimeSinceStartupAsDouble、秒）。未計測時は負値。
        /// シーンを跨いで GameMain 側が遷移所要時間の算出に使用します。
        /// </summary>
        public static double TransitionStartTime { get; private set; } = -1.0;

        /// <summary>戦闘シーンへの遷移開始時刻を記録します（フィールド側で遷移開始時に呼ぶ）。</summary>
        public static void MarkBattleTransitionStart()
        {
            TransitionStartTime = Time.realtimeSinceStartupAsDouble;
        }

        /// <summary>遷移所要時間の計測値のみをリセットします（IsFromField 等は保持）。</summary>
        public static void ClearTransitionStartTime()
        {
            TransitionStartTime = -1.0;
        }

        /// <summary>遷移先で読み込むステージインデックス。FilePathRegistry.StageNames[] のインデックス。</summary>
        public static int StageIndex { get; private set; } = 0;

        /// <summary>クリアしたノードのId。GameMain 終了後に FieldScene が読み取る。</summary>
        public static int ClearedNodeId { get; private set; } = -1;

        /// <summary>フィールドから戦闘・雇用などの別シーンへ遷移する前に呼ぶ。</summary>
        public static void SetupFieldExitTransition( int nodeId, int stageIndex = 0 )
        {
            IsFromField   = true;
            StageIndex    = stageIndex;
            ClearedNodeId = nodeId;
        }

        /// <summary>フィールドへ帰還したとき、または直接起動したときに呼んでリセットする。</summary>
        public static void Clear()
        {
            IsFromField         = false;
            StageIndex          = 0;
            ClearedNodeId       = -1;
            TransitionStartTime = -1.0;
        }
    }
}
