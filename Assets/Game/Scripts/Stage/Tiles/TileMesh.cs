using System.Collections;
using UnityEngine;
using static Constants;

namespace Frontier.Stage
{
    /// <summary>
    /// 移動可能、攻撃可能など、タイルに対する情報を表示します
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class TileMesh : MonoBehaviour
    {
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private Coroutine _blinkCoroutine = null;

        private const float BLINK_INTERVAL = 0.35f;

        /// <summary>このタイルメッシュが属するタイルマップ種別</summary>
        public TileMapType MapType { get; set; } = TileMapType.NONE;

        void Awake()
        {
            _meshFilter      = GetComponent<MeshFilter>();
            _meshRenderer    = GetComponent<MeshRenderer>();
        }

        /// <summary>
        /// タイルメッシュを描画します
        /// </summary>
        /// <param name="position"></param>
        /// <param name="collectedPosY"></param>
        /// <param name="tileSize"></param>
        /// <param name="color"></param>
        public void DrawTileMesh( in Vector3 position, float collectedPosY, in Color color )
        {
            var mesh        = new Mesh();
            float halfSize  = 0.5f * TILE_SIZE;
            // 頂点座標配列をメッシュにセット
            mesh.SetVertices( new Vector3[] {
                new Vector3 (position.x - halfSize, position.y + collectedPosY, position.z - halfSize),
                new Vector3 (position.x - halfSize, position.y + collectedPosY, position.z + halfSize),
                new Vector3 (position.x + halfSize, position.y + collectedPosY, position.z + halfSize),
                new Vector3 (position.x + halfSize, position.y + collectedPosY, position.z - halfSize),
            } );

            // インデックス配列をメッシュにセット
            mesh.SetTriangles( new int[]
            { 0, 1, 2, 0, 2, 3}, 0
            );

            _meshRenderer.material.color = color;
            _meshFilter.sharedMesh       = mesh;  // MeshFilterを通してメッシュをMeshRendererにセット
        }

        /// <summary>
        /// メッシュの描画を消去します
        /// </summary>
        public void ClearDraw()
        {
            _meshFilter.sharedMesh = null;
        }

        /// <summary>
        /// ゲームオブジェクトを削除します
        /// </summary>
        public void Remove()
        {
            Destroy(gameObject);
            Destroy(this);
        }

        public Color GetColor()
        {
            return _meshRenderer.material.color;
        }

        /// <summary>
        /// タイルメッシュの点滅を開始・停止します。
        /// 停止時はレンダラーを可視状態に戻します。
        /// </summary>
        public void SetBlink( bool isBlink )
        {
            if( isBlink )
            {
                if( _blinkCoroutine == null )
                {
                    _blinkCoroutine = StartCoroutine( BlinkCoroutine() );
                }
            }
            else
            {
                if( _blinkCoroutine != null )
                {
                    StopCoroutine( _blinkCoroutine );
                    _blinkCoroutine = null;
                }
                _meshRenderer.enabled = true;
            }
        }

        private IEnumerator BlinkCoroutine()
        {
            while( true )
            {
                _meshRenderer.enabled = !_meshRenderer.enabled;
                yield return new WaitForSeconds( BLINK_INTERVAL );
            }
        }
    }
}