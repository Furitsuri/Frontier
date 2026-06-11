using UnityEngine;
using static Constants;

namespace Frontier.Entities
{
    /// <summary>
    /// ステージ上に配置される装飾・環境オブジェクト（小屋・木・丸太など）の基底クラスです。
    /// Character と異なりアニメーションや戦闘ロジックを持たず、タイル上への静的配置が主な用途です。
    /// </summary>
    public class StageProp : Entity
    {
        private int _gridX = 0;
        private int _gridY = 0;

        public int GridX => _gridX;
        public int GridY => _gridY;

        /// <summary>
        /// グリッド座標とタイル上面の高さを指定してプロップを初期化します。
        /// </summary>
        /// <param name="gridX">グリッドX座標</param>
        /// <param name="gridY">グリッドY座標</param>
        /// <param name="tileTopHeight">配置するタイル上面のワールドY座標</param>
        public void Init( int gridX, int gridY, float tileTopHeight )
        {
            _gridX = gridX;
            _gridY = gridY;

            // タイルと同じ計算式でグリッド中心のワールド座標を求め、タイル上面に配置する
            Vector3 position = new Vector3(
                gridX * TILE_SIZE + 0.5f * TILE_SIZE,
                tileTopHeight,
                gridY * TILE_SIZE + 0.5f * TILE_SIZE
            );
            _transformHdlr.SetPosition( in position );

            base.Init();
        }
    }
}
