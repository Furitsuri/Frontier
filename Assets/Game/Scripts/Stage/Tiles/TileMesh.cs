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
        private CharacterKey _ownerKey;

        public CharacterKey OwnerKey => _ownerKey;

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
        public void DrawTileMesh( in Vector3 position, float collectedPosY, in Color color, in CharacterKey ownerKey )
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

            _meshRenderer.material.color    = color;
            _meshFilter.sharedMesh          = mesh;  // MeshFilterを通してメッシュをMeshRendererにセット  
            _ownerKey                       = ownerKey;
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
    }
}