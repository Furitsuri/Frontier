namespace Frontier.Combat
{
    /// <summary>
    /// スキルなどに用いられる射程のタイプです。
    /// 移動などと同等の通常範囲や直線範囲などが定義されています。
    /// </summary>
    public enum RangeType
    {
        None = -1,

        NORMAL,
        SINGLE_TARGET,
        LINE,
    }
}