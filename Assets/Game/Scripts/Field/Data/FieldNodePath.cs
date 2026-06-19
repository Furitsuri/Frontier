using System;

namespace Frontier.Field
{
    /// <summary>
    /// ノード間の経路定義。ベジェ制御点を持てる。
    /// CtrlX/CtrlY が両方 0 の場合は直線として扱う。
    /// </summary>
    [Serializable]
    public class FieldNodePath
    {
        public int   ToId;
        public float CtrlX;
        public float CtrlY;
    }
}
