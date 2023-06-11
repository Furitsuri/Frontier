using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class GridMesh : MonoBehaviour
{
    public enum MeshType
    {
        MOVE = 0,
        ATTACK,

        NUM_MAX
    }

    MeshFilter meshFilter;
    MeshRenderer meshRenderer;

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        // ステージグリッドに自身を登録
        StageGrid.instance.AddGridMeshToList(this);
    }

    // グリッドのメッシュを描画
    public void DrawGridMesh(ref Vector3 position, float gridSize, MeshType meshType)
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

        Color[] colors = new Color[]
        {
            new Color(0f, 0f, 1f, 0.65f),
            new Color(1f, 0f, 0f, 0.65f),
        };
        Debug.Assert( colors.Length == (int)MeshType.NUM_MAX, "Mesh type num is incorrect." );

        // タイプによって色を変更
        meshRenderer.material.color = colors[(int)meshType];

        // MeshFilterを通してメッシュをMeshRendererにセット  
        meshFilter.sharedMesh = mesh;
    }

    public void ClearDraw()
    {
        meshFilter.sharedMesh = null;
    }
}
