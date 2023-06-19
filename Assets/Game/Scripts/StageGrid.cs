using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Build.Content;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.TextCore.Text;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class StageGrid : Singleton<StageGrid>
{
    public struct GridInfo
    {
        // �L�����N�^�[�̗����ʒu���W
        public Vector3 charaStandPos;
        // �ړ��̉�
        public bool isMoveable;
        // �U���̉�
        public bool isAttackable;
        // �O���b�h��ɑ��݂���L�����̃C���f�b�N�X
        public int charaIndex;
        // ������
        // TODO : C# 10.0 ����͈����Ȃ��R���X�g���N�^�Œ�`�\(2023.5���_�̍ŐVUnity�o�[�W�����ł͎g�p�ł��Ȃ�)
        public void Init()
        {
            charaStandPos = Vector3.zero;
            isMoveable = false;
            isAttackable = false;
            charaIndex = -1;
        }
    }

    // �L�����N�^�[�̈ʒu�����ɖ߂��ۂɎg�p���܂�
    public struct Footprint
    {
        public int gridIndex;
        public Quaternion rotation;
    }

    public enum Face
    {
        xy,
        zx,
        yz,
    };

    [SerializeField]
    [HideInInspector]
    private int _gridNumX;
    [SerializeField]
    [HideInInspector]
    private int _gridNumZ;
    public GameObject m_StageObject;
    public GameObject m_GridMeshObject;
    public float gridSize = 1f;
    public UnityEngine.Color color = UnityEngine.Color.white;
    public Face face = Face.zx;
    public bool back = true;
    public bool isAdjustStageScale = false;

    private int _gridTotalNum = 0;
    private float _widthX, _widthZ;
    private Mesh _mesh;
    private GridInfo[] _gridInfo;
    private Footprint _footprint;
    private List<GridMesh> _gridMeshs;
    private List<int> _attackableGridIndexs;

    public CurrentGrid currentGrid { get; private set; } = CurrentGrid.GetInstance();

    override protected void Init()
    {
        // �o�g���}�l�[�W���ɓo�^
        BattleManager.Instance.registStageGrid(this);

        _gridMeshs          = new List<GridMesh>();
        _attackableGridIndexs  = new List<int>();

        // �X�e�[�W��񂩂�e�T�C�Y���Q�Ƃ���
        if (isAdjustStageScale)
        {
            _gridNumX = (int)(Math.Floor(m_StageObject.GetComponent<Renderer>().bounds.size.x) / gridSize);
            _gridNumZ = (int)(Math.Floor(m_StageObject.GetComponent<Renderer>().bounds.size.z) / gridSize);
        }

        // ���b�V����`��
        GetComponent<MeshFilter>().mesh = _mesh = new Mesh();
        _mesh = ReGrid(_mesh);

        // �O���b�h���̏�����
        InitGridInfo();

        currentGrid.Init(0, _gridNumX, _gridNumZ);
    }

    Mesh ReGrid(Mesh _mesh)
    {
        if (back)
        {
            GetComponent<MeshRenderer>().material = new Material(Shader.Find("Sprites/Default"));
        }
        else
        {
            GetComponent<MeshRenderer>().material = new Material(Shader.Find("GUI/Text Shader"));
        }

        _mesh.Clear();

        int resolution;
        int count = 0;
        Vector3[] vertices;
        Vector2[] uvs;
        int[] lines;
        UnityEngine.Color[] colors;

        _widthX                  = gridSize * _gridNumX / 2.0f;
        _widthZ                  = gridSize * _gridNumZ / 2.0f;
        Vector2 startPosition   = new Vector2(-_widthX, -_widthZ);
        Vector2 endPosition     = -startPosition;
        resolution              = 2 * (_gridNumX + _gridNumZ + 2);
        vertices                = new Vector3[resolution];
        uvs                     = new Vector2[resolution];
        lines                   = new int[resolution];
        colors                  = new UnityEngine.Color[resolution];

        // X�����̒��_
        for (int i = 0; count < 2 * (_gridNumX + 1); ++i, count = 2 * i)
        {
            vertices[count]     = new Vector3(startPosition.x + ((float)i * gridSize), startPosition.y, 0);
            vertices[count + 1] = new Vector3(startPosition.x + ((float)i * gridSize), endPosition.y, 0);
        }
        // Y(Z)�����̒��_
        for (int i = 0; count < resolution; ++i, count = 2 * i + 2 * (_gridNumX + 1))
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

        _mesh.vertices = RotationVertices(vertices, rotDirection);
        _mesh.uv = uvs;
        _mesh.colors = colors;
        _mesh.SetIndices(lines, MeshTopology.Lines, 0);

        return _mesh;
    }

    /// <summary>
    /// �O���b�h�������������܂�
    /// </summary>
    void InitGridInfo()
    {
        _gridTotalNum    = _gridNumX * _gridNumZ;
        _gridInfo        = new GridInfo[_gridTotalNum];

        for (int i = 0; i < _gridTotalNum; ++i)
        {
            // ������
            _gridInfo[i].Init();
            // �O���b�h�ʒu����L�����̗����ʒu�ւ̕␳�l
            float charaPosCorrext = 0.5f * gridSize;
            // 1�����z��Ńf�[�^����������, ��(X��)�����͏�]�ōl������
            float posX = -_widthX + i % _gridNumX * gridSize + charaPosCorrext;
            // 1�����z��Ńf�[�^����������, �c(Z��)�����͏��ōl������
            float posZ = -_widthZ + i / _gridNumX * gridSize + charaPosCorrext;
            // ��L�l����e�O���b�h�̃L�����̗����ʒu������
            _gridInfo[i].charaStandPos = new Vector3(posX, 0, posZ);
        }
    }

    /// <summary>
    /// �ړ��\�ȃO���b�h��o�^���܂�
    /// </summary>
    /// <param name="gridIndex">�o�^�Ώۂ̃O���b�h�C���f�b�N�X</param>
    /// <param name="moveableRange">�ړ��\�͈͒l</param>
    void RegistMoveableGrid(int gridIndex, int moveableRange, int attackableRange)
    {
        // ���̒l�ɂȂ�ΏI��
        if (moveableRange < 0) return;
        // �͈͊O�̃O���b�h�͍l�����Ȃ�
        if (gridIndex < 0 || _gridTotalNum <= gridIndex) return;

        _gridInfo[gridIndex].isMoveable = true;

        // �ړ��͈͂̒[�̉ӏ��ōU�������W��W�J���āA�U���͈͂ɂ��Ă��o�^����
        if (moveableRange == 0)
        {
            RegistAttackableGrid(gridIndex, attackableRange, attackableRange);

            return;
        }

        // ���[�����O
        if ( gridIndex%_gridNumX != 0 )
            RegistMoveableGrid(gridIndex - 1, moveableRange - 1, attackableRange);      // gridIndex����X��������-1
        // �E�[�����O
        if( ( gridIndex + 1 )%_gridNumX != 0 )
            RegistMoveableGrid(gridIndex + 1, moveableRange - 1, attackableRange);      // gridIndex����X��������+1
        // Z�������ւ̉��Z�ƌ��Z�͂��̂܂�
        RegistMoveableGrid(gridIndex - _gridNumX, moveableRange - 1, attackableRange);  // gridIndex����Z��������-1
        RegistMoveableGrid(gridIndex + _gridNumX, moveableRange - 1, attackableRange);  // gridIndex����Z��������+1
    }

    /// <summary>
    /// �U���\�ȃO���b�h��o�^���܂�
    /// </summary>
    /// <param name="gridIndex">�o�^�Ώۂ̃O���b�h�C���f�b�N�X</param>
    /// <param name="atkRangeMin">�U���\�͈͂̍ŏ��l</param>
    /// <param name="atkRangeMax">�U���\�͈͂̍ő�l</param>
    void RegistAttackableGrid(int gridIndex, int atkRangeMin, int atkRangeMax)
    {
        // ���̒l�ɂȂ�ΏI��
        if (atkRangeMax < 0) return;
        // �͈͊O�̃O���b�h�͍l�����Ȃ�
        if (gridIndex < 0 || _gridTotalNum <= gridIndex) return;
        // �U���ŏ������W�̒l��1�����̏�Ԃ̃O���b�h�̂ݍU���\
        if( atkRangeMin < 1 )
        {
            _gridInfo[gridIndex].isAttackable = true;
        }

        // ���[�����O
        if (gridIndex % _gridNumX != 0)
            RegistAttackableGrid(gridIndex - 1, atkRangeMin - 1, atkRangeMax - 1);    // gridIndex����X��������-1
        // �E�[�����O
        if ((gridIndex + 1) % _gridNumX != 0)
            RegistAttackableGrid(gridIndex + 1, atkRangeMin - 1, atkRangeMax - 1);    // gridIndex����X��������+1
        // Z�������ւ̉��Z�ƌ��Z�͂��̂܂�
        RegistAttackableGrid(gridIndex - _gridNumX, atkRangeMin - 1, atkRangeMax - 1); // gridIndex����Z��������-1
        RegistAttackableGrid(gridIndex + _gridNumX, atkRangeMin - 1, atkRangeMax - 1); // index����Z��������+1
    }

    /// <summary>
    /// ���_�z��f�[�^�����ׂĎw��̕����։�]�ړ������܂�
    /// </summary>
    /// <param name="vertices">��]�����钸�_�z��f�[�^</param>
    /// <param name="rotDirection">��]����</param>
    /// <returns>��]���������_�z��f�[�^</returns>
    Vector3[] RotationVertices(Vector3[] vertices, Vector3 rotDirection)
    {
        Vector3[] ret = new Vector3[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            ret[i] = Quaternion.LookRotation(rotDirection) * vertices[i];
        }
        return ret;
    }

    /// <summary>
    /// ���݂̃O���b�h���L�[���͂ő��삵�܂�
    /// </summary>
    public void OperateCurrentGrid()
    {
        // �U���t�F�[�Y��Ԃł͍U���\�ȃL�����N�^�[�����E�őI������
        if (BattleManager.Instance.IsAttackPhaseState())
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow))    { currentGrid.TransitPrevTarget(); }
            if (Input.GetKeyDown(KeyCode.RightArrow))   { currentGrid.TransitNextTarget(); }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))      { currentGrid.Up(); }
            if (Input.GetKeyDown(KeyCode.DownArrow))    { currentGrid.Down(); }
            if (Input.GetKeyDown(KeyCode.LeftArrow))    { currentGrid.Left(); }
            if (Input.GetKeyDown(KeyCode.RightArrow))   { currentGrid.Right(); }
        }
    }

    // �e�Z����Ԃ̕`��
    /// <summary>
    /// �ړ��\�O���b�h��`�悵�܂�
    /// </summary>
    /// <param name="departIndex">�ړ��L�����N�^�[�����݂���O���b�h�̃C���f�b�N�X�l</param>
    /// <param name="moveableRange">�ړ��\�͈͒l</param>
    /// <param name="attackableRange">�U���\�͈͒l</param>
    public void DrawMoveableGrids(int departIndex, int moveableRange, int attackableRange)
    {
        if (departIndex < 0 || _gridTotalNum <= departIndex)
        {
            Debug.Assert(false, "StageGrid : Irregular Index.");
            departIndex = 0;
        }

        // �S�ẴO���b�h�̈ړ��ۏ���������
        for (int i = 0; i < _gridTotalNum; ++i)
        {
            _gridInfo[i].isMoveable = false;
        }

        // �ړ��ۏ����e�O���b�h�ɓo�^
        RegistMoveableGrid(departIndex, moveableRange, attackableRange);
        // ���S�O���b�h������
        _gridInfo[departIndex].isMoveable = false;
        _gridInfo[departIndex].isAttackable = false;

        int count = 0;
        // �O���b�h�̏�Ԃ����b�V���ŕ`��
        for (int i = 0; i < _gridTotalNum; ++i)
        {
            if (_gridInfo[i].isMoveable)
            {
                Instantiate(m_GridMeshObject);  // ��
                _gridMeshs[count++].DrawGridMesh(ref _gridInfo[i].charaStandPos, gridSize, GridMesh.MeshType.MOVE);

                Debug.Log("Moveable Grid Index : " + i);
            }

            if (!_gridInfo[i].isMoveable && _gridInfo[i].isAttackable)
            {
                Instantiate(m_GridMeshObject);  // ��
                _gridMeshs[count++].DrawGridMesh(ref _gridInfo[i].charaStandPos, gridSize, GridMesh.MeshType.ATTACK);

                Debug.Log("Attackable Grid Index : " + i);
            }
        }
    }

    /// <summary>
    /// �U���\�O���b�h��`�悵�܂�
    /// </summary>
    /// <param name="departIndex">�U���L�����N�^�[�����݂���O���b�h�̃C���f�b�N�X�l</param>
    /// <param name="attackableRangeMin">�U���\�͈͂̍ŏ��l</param>
    /// <param name="attackableRangeMax">�U���\�͈͂̍ő�l</param>
    public void DrawAttackableGrids(int departIndex, int attackableRangeMin, int attackableRangeMax)
    {
        if (departIndex < 0 || _gridTotalNum <= departIndex)
        {
            Debug.Assert(false, "StageGrid : Irregular Index.");
            departIndex = 0;
        }

        // �S�ẴO���b�h�̍U���ۏ���������
        for (int i = 0; i < _gridTotalNum; ++i)
        {
            _gridInfo[i].isAttackable = false;
        }

        // �ړ��ۏ����e�O���b�h�ɓo�^
        RegistAttackableGrid(departIndex, attackableRangeMin, attackableRangeMax);

        int count = 0;
        // �O���b�h�̏�Ԃ����b�V���ŕ`��
        for (int i = 0; i < _gridTotalNum; ++i)
        {
            if (_gridInfo[i].isAttackable)
            {
                Instantiate(m_GridMeshObject);  // TODO : ��
                _gridMeshs[count++].DrawGridMesh(ref _gridInfo[i].charaStandPos, gridSize, GridMesh.MeshType.ATTACK);

                Debug.Log("Attackable Grid Index : " + i);
            }
        }
    }

    /// <summary>
    /// �S�ẴO���b�h�̉ۏ������������A�`����������܂�
    /// </summary>
    public void ClearGridsCondition()
    {
        // �S�ẴO���b�h�̈ړ��E�U���ۏ���������
        for (int i = 0; i < _gridTotalNum; ++i)
        {
            _gridInfo[i].isMoveable     = false;
            _gridInfo[i].isAttackable   = false;
        }

        foreach( var grid in _gridMeshs )
        {
            grid.ClearDraw();
        }
    }

    public void ClearGridsCharaIndex()
    {
        for (int i = 0; i < _gridTotalNum; ++i)
        {
            _gridInfo[i].charaIndex = -1;
        }
    }

    public void AddGridMeshToList( GridMesh script )
    {
        _gridMeshs.Add( script );
    }

    public ref Vector3 getGridCharaStandPos( int index )
    {
        return ref _gridInfo[index].charaStandPos;
    }

    /// <summary>
    /// ���ݑI�����Ă���O���b�h�̏����擾���܂�
    /// �U���ΏۑI����Ԃł͑I�����Ă���U���Ώۂ����݂���O���b�h�����擾���܂�
    /// </summary>
    /// <returns>�Y������O���b�h�̏��</returns>
    public ref GridInfo GetCurrentGridInfo()
    {
        int index = 0;

        if(BattleManager.Instance.IsAttackPhaseState())
        {
            index = _attackableGridIndexs[currentGrid.GetAtkTargetIndex()];
        }
        else
        {
            index = currentGrid.GetIndex();
        }

        return ref _gridInfo[ index ];
    }


    /// <summary>
    /// �w��C���f�b�N�X�̃O���b�h�����擾���܂�
    /// </summary>
    /// <param name="index">�w�肷��C���f�b�N�X�l</param>
    /// <returns>�w��C���f�b�N�X�̃O���b�h���</returns>
    public ref GridInfo GetGridInfo( int index )
    {
        return ref _gridInfo[index];
    }

    /// <summary>
    /// �o���n�_�ƖړI�n����ړ��o�H�ƂȂ�O���b�h�̃C���f�b�N�X���X�g���擾���܂�
    /// </summary>
    /// <param name="departGridIndex">�o���n�O���b�h�̃C���f�b�N�X</param>
    /// <param name="destGridIndex">�ړI�n�O���b�h�̃C���f�b�N�X</param>
    public List<int> ExtractDepart2DestGoalGridIndexs(int departGridIndex, int destGridIndex)
    {
        List<int> pathIndexs = new List<int>(64);

        // �X�^�[�g�n�_���p�X���ɒǉ�����
        pathIndexs.Add(departGridIndex);

        // �ړ��\�O���b�h�̂ݔ����o��
        for ( int i = 0; i < _gridInfo.Length; ++i )
        {
            if (_gridInfo[i].isMoveable )
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
                if ( (diff == -1 && (pathIndexs[i] % _gridNumX != 0) ) ||           // ���ɑ���(���[������)
                     (diff == 1 && (pathIndexs[i] % _gridNumX != _gridNumX - 1)) ||  // �E�ɑ���(�E�[������)
                      Math.Abs(diff) == _gridNumX)                                  // ��܂��͉��ɑ���
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

    /// <summary>
    /// �U���\�O���b�h�̂����A�U���\�L�����N�^�[�����݂���O���b�h�����X�g�ɓo�^���܂�
    /// </summary>
    /// <param name="targetTag">�U���Ώۂ̃^�O</param>
    /// <returns>�U���\�L�����N�^�[�����݂��Ă���</returns>
    public bool RegistAttackTargetGridIndexs( Character.CHARACTER_TAG targetTag )
    {
        Character character = null;
        var btlInstance = BattleManager.Instance;

        currentGrid.ClearAtkTargetInfo();
        _attackableGridIndexs.Clear();

        // �U���\�A���U���ΏۂƂȂ�L�����N�^�[�����݂���O���b�h�����X�g�ɓo�^
        for (int i = 0; i < _gridInfo.Length; ++i)
        {
            var info = _gridInfo[i];
            if (info.isAttackable)
            {
                character = btlInstance.SearchCharacterFromCharaIndex(info.charaIndex);
                if (character != null && character.param.charaTag == targetTag )
                {
                    _attackableGridIndexs.Add(i);
                }
            }
        }

        // �I���O���b�h�������I�ɍU���\�L�����N�^�[�̑��݂���O���b�h�C���f�b�N�X�ɐݒ�
        if( 0 < _attackableGridIndexs.Count )
        {
            currentGrid.SetAtkTargetNum( _attackableGridIndexs.Count );
            currentGrid.SetAtkTargetIndex(0);
        }

        return 0 < _attackableGridIndexs.Count;
    }

    /// <summary>
    /// �L�����N�^�[�̈ʒu�y�ь�����ێ����܂�
    /// </summary>
    /// <param name="footprint">�ێ�����l</param>
    public void LeaveFootprint( Footprint footprint )
    {
        _footprint = footprint;
    }

    /// <summary>
    /// �ێ����Ă����ʒu�y�ь������w��̃L�����N�^�[�ɐݒ肵�܂�
    /// </summary>
    /// <param name="character">�w�肷��L�����N�^�[</param>
    public void FollowFootprint(Character character)
    {
        currentGrid.SetIndex(_footprint.gridIndex);
        character.tmpParam.gridIndex = _footprint.gridIndex;
        character.transform.position = GetCurrentGridInfo().charaStandPos;
        character.transform.rotation = _footprint.rotation;
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
            script._gridNumX = EditorGUILayout.IntField("X�����O���b�h��", script._gridNumX);
            script._gridNumZ = EditorGUILayout.IntField("Z�����O���b�h��", script._gridNumZ);
            EditorGUI.EndDisabledGroup();

            base.OnInspectorGUI();
        }
    }
#endif // UNITY_EDITOR
}
