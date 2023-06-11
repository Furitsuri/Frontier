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
        // �X�e�[�W�O���b�h�Ɏ��g��o�^
        StageGrid.instance.AddGridMeshToList(this);
    }

    // �O���b�h�̃��b�V����`��
    public void DrawGridMesh(ref Vector3 position, float gridSize, MeshType meshType)
    {
        var mesh       = new Mesh();
        float halfSize = 0.5f * gridSize;
        // ���_���W�z������b�V���ɃZ�b�g  
        mesh.SetVertices(new Vector3[] {
            new Vector3 (position.x - halfSize, position.y + Constants.ADD_GRID_POS_Y, position.z - halfSize),
            new Vector3 (position.x - halfSize, position.y + Constants.ADD_GRID_POS_Y, position.z + halfSize),
            new Vector3 (position.x + halfSize, position.y + Constants.ADD_GRID_POS_Y, position.z + halfSize),
            new Vector3 (position.x + halfSize, position.y + Constants.ADD_GRID_POS_Y, position.z - halfSize),
        });

        // �C���f�b�N�X�z������b�V���ɃZ�b�g  
        mesh.SetTriangles(new int[]
        { 0, 1, 2, 0, 2, 3}, 0
        );

        Color[] colors = new Color[]
        {
            new Color(0f, 0f, 1f, 0.65f),
            new Color(1f, 0f, 0f, 0.65f),
        };
        Debug.Assert( colors.Length == (int)MeshType.NUM_MAX, "Mesh type num is incorrect." );

        // �^�C�v�ɂ���ĐF��ύX
        meshRenderer.material.color = colors[(int)meshType];

        // MeshFilter��ʂ��ă��b�V����MeshRenderer�ɃZ�b�g  
        meshFilter.sharedMesh = mesh;
    }

    public void ClearDraw()
    {
        meshFilter.sharedMesh = null;
    }
}
