using Frontier.Entities;
using Frontier.Stage;

namespace Frontier.Stage
{
    /// <summary>
    /// タイルの移動コストテーブルを管理するクラス
    /// </summary>
    static public class TileCostTables
    {
        /// <summary>
        /// デフォルトの移動コストテーブル
        /// </summary>
        static public readonly int[] defaultCostTable = new int[( int ) TileType.NUM]
        {
            1,  // None
            1,  // Grass
            1,  // Plant
            1,  // Wasteland
            2,  // Sand
            2,  // Water
            1,  // Mountain
            1   // Forest
        };

        /// <summary>
        /// 浮遊状態の移動コストテーブル
        /// </summary>
        static public readonly int[] floatingCostTable = new int[( int ) TileType.NUM]
        {
            1,  // None
            1,  // Grass
            1,  // Plant
            1,  // Wasteland
            1,  // Sand
            1,  // Water
            1,  // Mountain
            1   // Forest
        };

        /// <summary>
        /// 過重力状態の移動コストテーブル
        /// defaultCostTableの各値に+1したもの
        /// </summary>
        static public readonly int[] HyperGravityCostTable = new int[( int ) TileType.NUM]
        {
            defaultCostTable[(int)TileType.None] + 1,       // None
            defaultCostTable[(int)TileType.Grass] + 1,      // Grass
            defaultCostTable[(int)TileType.Plant] + 1,      // Plant
            defaultCostTable[(int)TileType.Wasteland] + 1,  // Wasteland
            defaultCostTable[(int)TileType.Sand] + 1,       // Sand
            defaultCostTable[(int)TileType.Water] + 1,      // Water
            defaultCostTable[(int)TileType.Mountain] + 1,   // Mountain
            defaultCostTable[(int)TileType.Forest] + 1      // Forest
        };
    }
}