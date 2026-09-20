namespace Frontier.Shop
{
    /// <summary>
    /// ShopHandler.Open() に渡すコンテキストです。中身は現時点では最小限とし、必要に応じて後で拡張します。
    /// </summary>
    public class ShopContext
    {
        /// <summary>
        /// 品揃え抽選のシード導出に使う固有ID(ステージ+商人インデックス、またはFieldノードID)。
        /// </summary>
        public int InstanceId { get; }

        /// <summary>
        /// 背景表示モード(戦闘中の暗転背景か、Field専用背景か)。
        /// </summary>
        public ShopBackgroundMode BackgroundMode { get; }

        public ShopContext( int instanceId, ShopBackgroundMode backgroundMode )
        {
            InstanceId     = instanceId;
            BackgroundMode = backgroundMode;
        }
    }
}
