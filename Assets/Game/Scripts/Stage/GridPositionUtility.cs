using UnityEngine;
using static Constants;

namespace Frontier.Stage
{
    /// <summary>
    /// グリッド上の座標計算に関する共通ユーティリティです。
    /// </summary>
    public static class GridPositionUtility
    {
        /// <summary>
        /// 基準タイルインデックスとサイズから、エンティティ（カーソル含む）が占有する
        /// タイル群の XZ 中心・Y 最高点を返します。
        /// size=1 の場合は基準タイルの CharaStandPos をそのまま返します。
        /// size>1 の場合は size×size 個のタイルの中心座標（Y は最大値）を返します。
        /// </summary>
        /// <param name="baseTileIndex">左上隅となる基準タイルインデックス</param>
        /// <param name="size">エンティティサイズ（1〜GRID_SIZE_MAX）</param>
        /// <param name="stageDataProvider">ステージデータプロバイダー</param>
        public static Vector3 CalcSizeAwareCenter( int baseTileIndex, int size, IStageDataProvider stageDataProvider )
        {
            var data = stageDataProvider.CurrentData;

            if ( size == 1 )
            {
                return data.GetTileStaticData( baseTileIndex ).CharaStandPos;
            }

            int     colNum  = data.TileColNum;
            Vector3 sumPos  = Vector3.zero;
            float   maxY    = float.MinValue;

            for ( int dy = 0; dy < size; dy++ )
            {
                for ( int dx = 0; dx < size; dx++ )
                {
                    int   idx     = baseTileIndex + dx + dy * colNum;
                    var   pos     = data.GetTileStaticData( idx ).CharaStandPos;
                    sumPos += pos;
                    if ( pos.y > maxY ) maxY = pos.y;
                }
            }

            int     count  = size * size;
            Vector3 center = sumPos / count;
            center.y = maxY;
            return center;
        }
    }
}
