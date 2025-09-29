using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier.Stage
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class GridMesh : MonoBehaviour
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
        public void DrawTileMesh( in Vector3 position, float tileSize, MeshType meshType )
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

            Color[] colors = new Color[ (int) MeshType.NUM ]
            {
                new Color(0f, 0f, 1f, 0.65f),   // 移動可能なタイル
                new Color(1f, 1f, 0f, 0.65f),   // 攻撃が到達可能な立ち位置となるタイル( TileBitFlag.REACHABLE_ATTACK )
                new Color(1f, 0f, 0f, 0.65f),   // 攻撃可能なタイル( TileBitFlag.ATTACKABLE )
                new Color(1f, 0f, 0f, 0.95f),   // 攻撃可能なタイルで、尚且つ攻撃対象が存在している( ATTACKABLE_TARGET_EXIST )
            };
            
            meshRenderer.material.color = colors[( int ) meshType]; // タイプによって色を変更
            meshFilter.sharedMesh       = mesh;                     // MeshFilterを通してメッシュをMeshRendererにセット  
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