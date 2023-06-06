using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Build.Content;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class StageGrid : MonoBehaviour
{
    public struct GridInfo
    {
        // �L�����N�^�[�̗����ʒu���W
        public Vector3 charaStandPos;
        // �ړ��̉�
        public bool    isMoveable;
        // �O���b�h��ɑ��݂���L�����̃C���f�b�N�X
        public int charaIndex;
        // ������
        // TODO : C# 10.0 ����͈����Ȃ��R���X�g���N�^�Œ�`�\(2023.5���_�̍ŐVUnity�o�[�W�����ł͎g�p�ł��Ȃ�)
        public void Init()
        {
            charaStandPos   = Vector3.zero;
            isMoveable      = false;
            charaIndex      = -1;
        }
    }

    public enum Face
    {
        xy,
        zx,
        yz,
    };

    public static StageGrid instance = null;

    [SerializeField]
    [HideInInspector]
    private int gridNumX;
    [SerializeField]
    [HideInInspector]
    private int gridNumZ;
    public GameObject m_StageObject;
    public GameObject m_GridMeshObject;
    public float gridSize           = 1f;
    public UnityEngine.Color color  = UnityEngine.Color.white;
    public Face face                = Face.zx;
    public bool back                = true;
    public bool isAdjustStageScale  = false;

    private int gridTotalNum = 0;
    private float widthX, widthZ;
    private Mesh mesh;
    private GridInfo[] gridInfo;
    private List<GridMesh> gridMeshs;

    public int CurrentGridIndex { get; private set; } = 0;

    void Awake()
    {
        // �C���X�^���X�̍쐬
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }

        DontDestroyOnLoad(gameObject);

        // �o�g���}�l�[�W���ɓo�^
        BattleManager.instance.registStageGrid(this);

        // �J���[�O���b�h�z��Ƀ��X�g���A�T�C��
        gridMeshs = new List<GridMesh>();

        // �X�e�[�W��񂩂�e�T�C�Y���Q�Ƃ���
        if (isAdjustStageScale)
        {
            gridNumX = (int)(Math.Floor(m_StageObject.GetComponent<Renderer>().bounds.size.x) / gridSize);
            gridNumZ = (int)(Math.Floor(m_StageObject.GetComponent<Renderer>().bounds.size.z) / gridSize);
        }

        // ���b�V����`��
        GetComponent<MeshFilter>().mesh = mesh = new Mesh();
        mesh = ReGrid(mesh);

        // �O���b�h���̏�����
        InitGridInfo();
    }

    Mesh ReGrid(Mesh mesh)
    {
        if (back)
        {
            GetComponent<MeshRenderer>().material = new Material(Shader.Find("Sprites/Default"));
        }
        else
        {
            GetComponent<MeshRenderer>().material = new Material(Shader.Find("GUI/Text Shader"));
        }

        mesh.Clear();

        int resolution;
        int count = 0;
        Vector3[] vertices;
        Vector2[] uvs;
        int[] lines;
        UnityEngine.Color[] colors;

        widthX                  = gridSize * gridNumX / 2.0f;
        widthZ                  = gridSize * gridNumZ / 2.0f;
        Vector2 startPosition   = new Vector2(-widthX, -widthZ);
        Vector2 endPosition     = -startPosition;
        resolution              = 2 * (gridNumX + gridNumZ + 2);
        vertices                = new Vector3[resolution];
        uvs                     = new Vector2[resolution];
        lines                   = new int[resolution];
        colors                  = new UnityEngine.Color[resolution];

        // X�����̒��_
        for (int i = 0; count < 2 * (gridNumX + 1); ++i, count = 2 * i)
        {
            vertices[count]     = new Vector3(startPosition.x + ((float)i * gridSize), startPosition.y, 0);
            vertices[count + 1] = new Vector3(startPosition.x + ((float)i * gridSize), endPosition.y, 0);
        }
        // Y(Z)�����̒��_
        for (int i = 0; count < resolution; ++i, count = 2 * i + 2 * (gridNumX + 1))
        {
            vertices[count]     = new Vector3(startPosition.x, endPosition.y - ((float)i * gridSize), 0);
            vertices[count + 1] = new Vector3(endPosition.x, endPosition.y - ((float)i * gridSize), 0);
        }

        for (int i = 0; i < resolution; i++)
        {
            uvs[i] = Vector2.zero;
            lines[i] = i;
            colors[i] = color;
        }

        Vector3 rotDirection;
        switch (face)
        {
            case Face.xy:
                rotDirection = Vector3.forward;
                break;
            case Face.zx:
                rotDirection = Vector3.up;
                break;
            case Face.yz:
                rotDirection = Vector3.right;
                break;
            default:
                rotDirection = Vector3.forward;
                break;
        }

        mesh.vertices = RotationVertices(vertices, rotDirection);
        mesh.uv = uvs;
        mesh.colors = colors;
        mesh.SetIndices(lines, MeshTopology.Lines, 0);

        return mesh;
    }

    // �O���b�h���̏�����
    void InitGridInfo()
    {
        gridTotalNum    = gridNumX * gridNumZ;
        gridInfo        = new GridInfo[gridTotalNum];

        for (int i = 0; i < gridTotalNum; ++i)
        {
            // ������
            gridInfo[i].Init();
            // �O���b�h�ʒu����L�����̗����ʒu�ւ̕␳�l
            float charaPosCorrext = 0.5f * gridSize;
            // 1�����z��Ńf�[�^����������, ��(X��)�����͏�]�ōl������
            float posX = -widthX + i % gridNumX * gridSize + charaPosCorrext;
            // 1�����z��Ńf�[�^����������, �c(Z��)�����͏��ōl������
            float posZ = -widthZ + i / gridNumX * gridSize + charaPosCorrext;
            // ��L�l����e�O���b�h�̃L�����̗����ʒu������
            gridInfo[i].charaStandPos = new Vector3(posX, 0, posZ);
        }
    }

    void RegistMoveableGrid(int index, int moveable)
    {
        // 1�����ɂȂ�ΏI��
        if (moveable <= 0) return;
        // �͈͊O�̃O���b�h�͍l�����Ȃ�
        if (index < 0 || gridTotalNum <= index) return;

        gridInfo[index].isMoveable = true;

        // ���[�����O
        if( index%gridNumX != 0 )
            RegistMoveableGrid(index - 1, moveable - 1);    // index����X��������-1
        // �E�[�����O
        if( ( index + 1 )%gridNumX != 0 )
            RegistMoveableGrid(index + 1, moveable - 1);    // index����X��������+1
        // Z�������ւ̉��Z�ƌ��Z�͂��̂܂�
        RegistMoveableGrid(index - gridNumX, moveable - 1); // index����Z��������-1
        RegistMoveableGrid(index + gridNumX, moveable - 1); // index����Z��������-1
    }

    //���_�z��f�[�^�[�����ׂĎw��̕����։�]�ړ�������
    Vector3[] RotationVertices(Vector3[] vertices, Vector3 rotDirection)
    {
        Vector3[] ret = new Vector3[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            ret[i] = Quaternion.LookRotation(rotDirection) * vertices[i];
        }
        return ret;
    }

    // ���݃O���b�h�̑���
    public void OperateCurrentGrid()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))      { CurrentGridIndex += gridNumX; }
        if (Input.GetKeyDown(KeyCode.DownArrow))    { CurrentGridIndex -= gridNumX; }
        if (Input.GetKeyDown(KeyCode.LeftArrow))    { CurrentGridIndex -= 1; }
        if (Input.GetKeyDown(KeyCode.RightArrow))   { CurrentGridIndex += 1; }
    }

    // �e�Z����Ԃ̕`��
    public void DrawGridsCondition(int index, int moveable, BattleManager.TurnType type)
    {
        if (index < 0 || gridTotalNum <= index)
        {
            Debug.Assert(false, "StageGrid : Irregular Index.\n");
            index = 0;
        }

        // �S�ẴO���b�h�̈ړ��ۏ���������
        for (int i = 0; i < gridTotalNum; ++i)
        {
            gridInfo[i].isMoveable = false;
        }

        // �ړ��ۏ����e�O���b�h�ɓo�^
        RegistMoveableGrid(index, moveable);
        // ���S�O���b�h������
        gridInfo[index].isMoveable = false;

        int count = 0;
        // �O���b�h�̏�Ԃ����b�V���ŕ`��
        for (int i = 0; i < gridTotalNum; ++i)
        {
            if (gridInfo[i].isMoveable)
            {
                Instantiate(m_GridMeshObject);  // ��
                gridMeshs[count++].DrawGridMesh(ref gridInfo[i].charaStandPos, gridSize, 0);

                Debug.Log("Moveable Grid Index : " + i);
            }
        }
    }

    public void clearGridsCondition()
    {
        // �S�ẴO���b�h�̈ړ��ۏ���������
        for (int i = 0; i < gridTotalNum; ++i)
        {
            gridInfo[i].isMoveable = false;
        }

        foreach( var grid in gridMeshs )
        {
            grid.ClearDraw();
        }
    }

    public void ClearGridsCharaIndex()
    {
        for (int i = 0; i < gridTotalNum; ++i)
        {
            gridInfo[i].charaIndex = -1;
        }
    }

    public void AddGridMeshToList( GridMesh script )
    {
        gridMeshs.Add( script );
    }

    public ref Vector3 getGridCharaStandPos( int index )
    {
        return ref gridInfo[index].charaStandPos;
    }

    // �I�𒆃O���b�h���̎擾
    public ref GridInfo getCurrentGridInfo()
    {
        return ref gridInfo[ CurrentGridIndex ];
    }

    // �w��O���b�h���̎擾
    public ref GridInfo getGridInfo( int index )
    {
        return ref gridInfo[index];
    }

    // �o���n�_�ƖړI�n����ړ��o�H�ƂȂ�O���b�h���擾
    public List<int> extractDepart2DestGoalGridIndexs(int departGridIndex, int destGridIndex)
    {
        List<int> pathIndexs = new List<int>(64);

        // �X�^�[�g�n�_���p�X���ɒǉ�����
        pathIndexs.Add(departGridIndex);

        // �ړ��\�O���b�h�̂ݔ����o��
        for ( int i = 0; i < gridInfo.Length; ++i )
        {
            if (gridInfo[i].isMoveable )
            {
                pathIndexs.Add(i);
            }
        }

        Dijkstra dijkstra = new Dijkstra(pathIndexs.Count);

        // �o���O���b�h����̃C���f�b�N�X�̍����擾
        for ( int i = 0; i + 1 < pathIndexs.Count; ++i )
        {
            for( int j = i + 1; j < pathIndexs.Count; ++j )
            {
                int diff = pathIndexs[j] - pathIndexs[i];
                if ( (diff == -1 && (pathIndexs[i] % gridNumX != 0) ) ||           // ���ɑ���(���[������)
                     (diff == 1 && (pathIndexs[i] % gridNumX != gridNumX - 1)) ||  // �E�ɑ���(�E�[������)
                      Math.Abs(diff) == gridNumX)                                  // ��܂��͉��ɑ���
                {
                    // �ړ��\�ȗאڃO���b�h�����_�C�N�X�g���ɓ����
                    dijkstra.Add(i, j);
                }
            }
        }

        // �_�C�N�X�g������o���O���b�h����ړI�O���b�h�܂ł̍ŒZ�o�H�𓾂�
        List<int> minRouteIndexs = dijkstra.GetMinRoute(pathIndexs.IndexOf(departGridIndex), pathIndexs.IndexOf(destGridIndex));
        for( int i = 0; i < minRouteIndexs.Count; ++i )
        {
            minRouteIndexs[i] = pathIndexs[ minRouteIndexs[i] ];
        }

        return minRouteIndexs;
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(StageGrid))]
    public class StageGridEditor : UnityEditor.Editor
    {
        override public void OnInspectorGUI()
        {
            StageGrid script = target as StageGrid;

            // �X�e�[�W��񂩂�T�C�Y�����߂�ۂ̓T�C�Y�ҏW��s�ɂ���
            EditorGUI.BeginDisabledGroup(script.isAdjustStageScale);
            script.gridNumX = EditorGUILayout.IntField("X�����O���b�h��", script.gridNumX);
            script.gridNumZ = EditorGUILayout.IntField("Z�����O���b�h��", script.gridNumZ);
            EditorGUI.EndDisabledGroup();

            base.OnInspectorGUI();
        }
    }
#endif // UNITY_EDITOR
}
