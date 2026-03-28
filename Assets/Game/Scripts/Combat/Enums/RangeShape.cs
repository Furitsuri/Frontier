namespace Frontier.Combat
{
    /// <summary>
    /// スキルなどに用いられる射程のタイプです。
    /// 移動などと同等の通常範囲や直線範囲などが定義されています。
    /// </summary>
    public enum RangeShape
    {
        NONE = -1,

        FROM_MYSELF,    // 自身中心
        LINEARLY,       // 直線状
        /// <summary>
        /// MEMO : 以下、必要になった際に定義してください。
        /// </summary>
        // RADIALLY,       // 放射状
        // SPECIAL,        // 特殊(スキル毎)

        NUM,
    }
}