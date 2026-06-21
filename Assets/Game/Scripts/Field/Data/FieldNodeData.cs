using System;

namespace Frontier.Field
{
    [Serializable]
    public class FieldNodeData
    {
        public int   Id;
        public int   Type;          // FieldNodeType
        public int   StageIndex;    // Battle/Boss のみ有効。FilePathRegistry.StageNames[] のインデックス
        public float PosX;
        public float PosY;

        /// <summary>
        /// このノードが属するレイヤー(Start=0からBossレイヤーまでの深さ)。
        /// -1 = ファイルで未指定(そのレイヤーはランダム生成の対象になりうる)。
        /// 0以上の値を明示すると、そのレイヤーは「Fixedレイヤー」として扱われ、ランダム生成されなくなる。
        /// </summary>
        public int Layer = -1;
        public int[]           NextIds;      // このノードから進める次ノードのId一覧
        public FieldNodePath[] PathToNext;   // 経路描画用（null の場合は NextIds から直線で補完）
    }
}
