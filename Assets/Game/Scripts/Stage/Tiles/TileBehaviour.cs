using Frontier.Stage;
using Unity.VisualScripting;
using UnityEngine;
using static Constants;

namespace Frontier.Stage
{
    public class TileBehaviour : MonoBehaviour, IDisposer
    {
        private MeshRenderer _renderer;

        void Awake()
        {
            _renderer = GetComponent<MeshRenderer>();

            // タイルのデフォルトスケールを設定
            transform.localScale = TileMaterialLibrary.GetDefaultTileScale();
        }

        /// <summary>
        /// 破棄処理を行います。
        /// </summary>
        public void Dispose()
        {
            if (_renderer != null)
            {
                Destroy(_renderer);
            }

            Destroy(gameObject);
            Destroy( this );
        }

        public void Init( int x, int y, float height, TileType type )
        {
            Vector3 position = new Vector3(
                x * TILE_SIZE + 0.5f * TILE_SIZE,   // X座標はグリッドの中心に配置
                0.5f * height - TILE_MIN_THICKNESS, // Y座標はタイルの高さ(タイルの厚みの最小値は減算する)
                y * TILE_SIZE + 0.5f * TILE_SIZE    // Z座標はグリッドの中心に配置
            );

            transform.position      = position;
            transform.localScale    = new Vector3( TILE_SIZE, height + TILE_MIN_THICKNESS, TILE_SIZE );
            transform.rotation      = Quaternion.identity;
            _renderer.material      = TileMaterialLibrary.GetMaterial( type );
        }

        public void ApplyTileType(TileType type)
        {
            _renderer.material = TileMaterialLibrary.GetMaterial(type);
            // transform.localScale = TileMaterialLibrary.GetScale(type); // 高さ表現もここで調整
        }
    }
}