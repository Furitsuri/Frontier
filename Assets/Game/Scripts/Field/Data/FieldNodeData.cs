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
        public int[] NextIds;       // このノードから進める次ノードのId一覧
    }
}
