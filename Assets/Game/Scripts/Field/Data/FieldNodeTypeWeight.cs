using System;

namespace Frontier.Field
{
    /// <summary>
    /// フィールド生成時の中間ノードタイプ抽選用の重み定義。
    /// </summary>
    [Serializable]
    public struct FieldNodeTypeWeight
    {
        public FieldNodeType Type;
        public float         Weight;
    }
}
