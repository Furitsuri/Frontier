namespace Frontier.Stage
{
    /// <summary>
    /// タイルマップの種別を表します。ビットフラグとして複数種を同時に指定できます。
    /// </summary>
    [System.Flags]
    public enum TileMapType : int
    {
        NONE       = 0,
        MOVEABLE   = 1 << 0,   // 移動可能タイル
        ATTACKABLE = 1 << 1,   // 攻撃可能タイル
        TARGETABLE = 1 << 2,   // ターゲット選択可能タイル
        QUEUED     = 1 << 3,   // 予約済みアクション表示タイル
    }
}
