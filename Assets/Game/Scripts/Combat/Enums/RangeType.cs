namespace Frontier.Combat
{
    /// <summary>
    /// スキルなどに用いられる射程のタイプです。
    /// 移動などと同等の通常範囲や直線範囲などが定義されています。
    /// </summary>
    public enum RangeType
    {
        NONE = -1,

        NORMAL,
        LINEARLY,

        NUM,
    }
}