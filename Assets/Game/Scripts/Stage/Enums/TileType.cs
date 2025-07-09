namespace Frontier.Stage
{
    /// <summary>
    /// タイルの種類を表す列挙型
    /// </summary>
    public enum TileType
    {
        None        = 0,    // なし
        Grass,              // 芝生
        Plant,              // 植物
        Wasteland,          // 荒地
        Sand,               // 砂
        Water,              // 水辺
        Mountain,           // 山
        Forest,             // 森

        NUM
    }
}