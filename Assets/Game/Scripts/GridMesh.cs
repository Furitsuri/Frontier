using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class GridMesh : MonoBehaviour
{
    MeshFilter meshFilter;

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        // ステージグリッドに自身を登録
        StageGrid.instance.AddGridMeshToList(this);
    }

    // グリッドのメッシュを描画
    public void DrawGridMesh(ref Vector3 position, float gridSize, int index)
    {
        var mesh       = new Mesh();
        float halfSize = 0.5f * gridSize;
        // 頂点座標配列をメッシュにセット  
        mesh.SetVertices(new Vector3[] {
            new Vector3 (position.x - halfSize, position.y + Constants.ADD_GRID_POS_Y, position.z - halfSize),
            new Vector3 (position.x - halfSize, position.y + Constants.ADD_GRID_POS_Y, position.z + halfSize),
            new Vector3 (position.x + halfSize, position.y + Constants.ADD_GRID_POS_Y, position.z + halfSize),
            new Vector3 (position.x + halfSize, position.y + Constants.ADD_GRID_POS_Y, position.z - halfSize),
        });

        // インデックス配列をメッシュにセット  
        mesh.SetTriangles(new int[]
        { 0, 1, 2, 0, 2, 3}, 0
        );

        // MeshFilterを通してメッシュをMeshRendererにセット  
        meshFilter.sharedMesh = mesh;
    }

    public void ClearDraw()
    {
        meshFilter.sharedMesh = null;
    }
}
