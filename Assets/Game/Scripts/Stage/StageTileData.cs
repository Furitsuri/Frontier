using Frontier.Stage;

namespace Frontier.Stage
{
    /// <summary>
    /// ステージ上のタイルデータ
    /// </summary>

    [System.Serializable]
    public class StageTileData
    {
        public int x;
        public int y;
        public TileType tileType;
    }
}