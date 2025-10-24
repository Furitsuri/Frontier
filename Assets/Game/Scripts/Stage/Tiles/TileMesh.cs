using UnityEngine;

namespace Frontier.Stage
{
    /// <summary>
    /// 移動可能、攻撃可能など、タイルに対する情報を表示します
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class TileMesh : MonoBehaviour
    {
        MeshFilter meshFilter;
        MeshRenderer meshRenderer;

        void Awake()
        {
            meshFilter      = GetComponent<MeshFilter>();
            meshRenderer    = GetComponent<MeshRenderer>();
        }

        /// <summary>
        /// グリッドのメッシュを描画します
        /// </summary>
        /// <param name="position">メッシュを描画する座標の中心点</param>
        /// <param name="tileSize">グリッドのサイズ</param>
        /// <param name="meshType">メッシュタイプ</param>
        public void DrawTileMesh( in Vector3 position, float tileSize, Color color )
        {
            var mesh        = new Mesh();
            float halfSize  = 0.5f * tileSize;
            // 頂点座標配列をメッシュにセット  
            mesh.SetVertices( new Vector3[] {
                new Vector3 (position.x - halfSize, position.y + Constants.ADD_GRID_POS_Y, position.z - halfSize),
                new Vector3 (position.x - halfSize, position.y + Constants.ADD_GRID_POS_Y, position.z + halfSize),
                new Vector3 (position.x + halfSize, position.y + Constants.ADD_GRID_POS_Y, position.z + halfSize),
                new Vector3 (position.x + halfSize, position.y + Constants.ADD_GRID_POS_Y, position.z - halfSize),
            } );

            // インデックス配列をメッシュにセット  
            mesh.SetTriangles( new int[]
            { 0, 1, 2, 0, 2, 3}, 0
            );

            meshRenderer.material.color = color;
            meshFilter.sharedMesh       = mesh;  // MeshFilterを通してメッシュをMeshRendererにセット  
        }

        /// <summary>
        /// メッシュの描画を消去します
        /// </summary>
        public void ClearDraw()
        {
            meshFilter.sharedMesh = null;
        }

        /// <summary>
        /// ゲームオブジェクトを削除します
        /// </summary>
        public void Remove()
        {
            Destroy(gameObject);
            Destroy(this);
        }
    }
}