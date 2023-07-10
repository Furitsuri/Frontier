using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Character;
using static EMAIBase;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class StageGrid : Singleton<StageGrid>
{
    /// <summary>
    /// �O���b�h�ɑ΂���t���O���
    /// </summary>
   public enum BitFlag
    {
        NONE = 0,
        CANNOT_MOVE         = 1 << 0,   // �ړ��s��
        ATTACKABLE          = 1 << 1,   // �U���\
        TARGET_ATTACK_BASE  = 1 << 2,   // �^�[�Q�b�g�U�����n�_
        PLAYER_EXIST        = 1 << 3,   // �v���C���[�L�����N�^�[������
        ENEMY_EXIST         = 1 << 4,   // �G�L�����N�^�[������
        OTHER_EXIST         = 1 << 5,   // ��O���͂�����
    }

    public struct GridInfo
    {
        // �L�����N�^�[�̗����ʒu���W(��)
        public Vector3 charaStandPos;
        // �ړ��j�Q�l(��)
        public int moveResist;
        // �ړ��l�̌��ς���l
        public int estimatedMoveRange;
        // �O���b�h��ɑ��݂���L�����N�^�[�̃^�C�v
        public Character.CHARACTER_TAG characterTag;
        // �O���b�h��ɑ��݂���L�����N�^�[�̃C���f�b�N�X
        public int charaIndex;
        // �t���O���
        public BitFlag flag;
        // �� ��x�ݒ肳�ꂽ��͕ύX���邱�Ƃ��Ȃ��ϐ�

        /// <summary>
        /// ���������܂�
        /// TODO�F�X�e�[�W�̃t�@�C���Ǎ��ɂ����moveRange���͂��߂Ƃ����l��ݒ�o����悤�ɂ�����
        ///       �܂��AC# 10.0 ����͈����Ȃ��R���X�g���N�^�Œ�`�\(2023.5���_�̍ŐVUnity�o�[�W�����ł͎g�p�ł��Ȃ�)
        /// </summary>
        public void Init()
        {
            charaStandPos           = Vector3.zero;
            moveResist              = -1;
            estimatedMoveRange      = -1;
            characterTag            = CHARACTER_TAG.CHARACTER_NONE;
            charaIndex              = -1;
            flag                    = BitFlag.NONE;
        }

        public bool IsExistCharacter()
        {
            return 0 <= charaIndex;
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
    private float _widthX, _widthZ;
    private Mesh _mesh;
    private GridInfo[] _gridInfo;
    private GridInfo[] _gridInfoBase;
    private Footprint _footprint;
    private List<GridMesh> _gridMeshs;
    private List<int> _attackableGridIndexs;
    private CurrentGrid _currentGrid = CurrentGrid.GetInstance();

    public int GridTotalNum { get; private set; } = 0;

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

        _currentGrid.Init(0, _gridNumX, _gridNumZ);
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
        GridTotalNum        = _gridNumX * _gridNumZ;
        _gridInfo           = new GridInfo[GridTotalNum];
        _gridInfoBase       = new GridInfo[GridTotalNum]; ;

        for (int i = 0; i < GridTotalNum; ++i)
        {
            // ������
            _gridInfo[i].Init();
            _gridInfoBase[i].Init();
            // �O���b�h�ʒu����L�����̗����ʒu�ւ̕␳�l
            float charaPosCorrext = 0.5f * gridSize;
            // 1�����z��Ńf�[�^����������, ��(X��)�����͏�]�ōl������
            float posX = -_widthX + i % _gridNumX * gridSize + charaPosCorrext;
            // 1�����z��Ńf�[�^����������, �c(Z��)�����͏��ōl������
            float posZ = -_widthZ + i / _gridNumX * gridSize + charaPosCorrext;
            // ��L�l����e�O���b�h�̃L�����̗����ʒu������
            _gridInfoBase[i].charaStandPos = _gridInfo[i].charaStandPos = new Vector3(posX, 0, posZ);
            // TODO : �t�@�C���ǂݍ��݂���ʍs�s�\�ȉӏ��Ȃǂ�BitFlag����ݒ�o����悤�ɂ���
        }
    }

    /// <summary>
    /// _gridInfo�̏�Ԃ���̏�Ԃɖ߂��܂�
    /// </summary>
    void ResetGridInfo()
    {
        for (int i = 0; i < GridTotalNum; ++i)
        {
            _gridInfo[i] = _gridInfoBase[i];
        }
    }

    /// <summary>
    /// �ړ��\�ȃO���b�h��o�^���܂�
    /// </summary>
    /// <param name="gridIndex">�o�^�Ώۂ̃O���b�h�C���f�b�N�X</param>
    /// <param name="moveRange">�ړ��\�͈͒l</param>
    /// <param name="attackRange">�U���\�͈͒l</param>
    /// <param name="selfTag">�Ăяo�����L�����N�^�[�̃L�����N�^�[�^�O</param>
    /// <param name="isAttackable">�Ăяo�����̃L�����N�^�[���U���\���ۂ�</param>
    /// <param name="isDeparture">�o���O���b�h����Ăяo���ꂽ���ۂ�</param>
    void RegistMoveableEachGrid(int gridIndex, int moveRange, int attackRange, Character.CHARACTER_TAG selfTag, bool isAttackable, bool isDeparture = false)
    {
        // �͈͊O�̃O���b�h�͍l�����Ȃ�
        if (gridIndex < 0 || GridTotalNum <= gridIndex) return;
        // �ړ��s�̃O���b�h�ɒH�蒅�����ꍇ�͏I��
        if (Methods.CheckBitFlag(_gridInfo[gridIndex].flag, BitFlag.CANNOT_MOVE)) return;
        // ���Ɍv�Z�ς݂̃O���b�h�ł���ΏI��
        if (moveRange <= _gridInfo[gridIndex].estimatedMoveRange) return;
        // ���g�ɑ΂���G�ΐ��̓L�����N�^�[�����݂���ΏI��
        StageGrid.BitFlag[] opponentTag = new StageGrid.BitFlag[(int)CHARACTER_TAG.CHARACTER_NUM]
        {
            BitFlag.ENEMY_EXIST  | BitFlag.OTHER_EXIST,     // PLAYER�ɂ�����G�ΐ���
            BitFlag.PLAYER_EXIST | BitFlag.OTHER_EXIST,     // ENEMY�ɂ�����G�ΐ���
            BitFlag.PLAYER_EXIST | BitFlag.ENEMY_EXIST      // OTHER�ɂ�����G�ΐ���
        };
        if (Methods.CheckBitFlag(_gridInfo[gridIndex].flag, opponentTag[(int)selfTag])) return;

        // ���݃O���b�h�̈ړ���R�l���X�V( �o���O���b�h�ł�moveRange�̒l�����̂܂ܓK������ )
        int currentMoveRange = (isDeparture) ? moveRange : _gridInfo[gridIndex].moveResist + moveRange;
        _gridInfo[gridIndex].estimatedMoveRange = currentMoveRange;

        // ���̒l�ł���ΏI��
        if (currentMoveRange < 0) return;

        // �U���͈͂ɂ��Ă��o�^����
        if (isAttackable && _gridInfo[gridIndex].characterTag == CHARACTER_TAG.CHARACTER_NONE)
            RegistAttackableEachGrid(gridIndex, attackRange, selfTag, gridIndex);
        // ���[�����O
        if ( gridIndex%_gridNumX != 0 )
            RegistMoveableEachGrid(gridIndex - 1, currentMoveRange, attackRange, selfTag, isAttackable);      // gridIndex����X��������-1
        // �E�[�����O
        if( ( gridIndex + 1 )%_gridNumX != 0 )
            RegistMoveableEachGrid(gridIndex + 1, currentMoveRange, attackRange, selfTag, isAttackable);      // gridIndex����X��������+1
        // Z�������ւ̉��Z�ƌ��Z�͂��̂܂�
        RegistMoveableEachGrid(gridIndex - _gridNumX, currentMoveRange, attackRange, selfTag, isAttackable);  // gridIndex����Z��������-1
        RegistMoveableEachGrid(gridIndex + _gridNumX, currentMoveRange, attackRange, selfTag, isAttackable);  // gridIndex����Z��������+1
    }

    /// <summary>
    /// �U���\�ȃO���b�h��o�^���܂�
    /// </summary>
    /// <param name="gridIndex">�Ώۂ̃O���b�h�C���f�b�N�X</param>
    /// <param name="attackRange">�U���\�͈͒l</param>
    /// <param name="selfTag">���g�̃L�����N�^�[�^�O</param>
    /// <param name="departIndex">�o���O���b�h�C���f�b�N�X</param>
    void RegistAttackableEachGrid(int gridIndex, int attackRange, Character.CHARACTER_TAG selfTag, int departIndex)
    {
        // �͈͊O�̃O���b�h�͍l�����Ȃ�
        if (gridIndex < 0 || GridTotalNum <= gridIndex) return;
        // �ړ��s�̃O���b�h�ɂ͍U���ł��Ȃ�
        if (Methods.CheckBitFlag(_gridInfo[gridIndex].flag, BitFlag.CANNOT_MOVE)) return;
        // �o���n�_�łȂ���Γo�^
        if ( gridIndex != departIndex)
        {
            Methods.SetBitFlag(ref _gridInfo[gridIndex].flag, BitFlag.ATTACKABLE);

            switch( selfTag )
            {
                case Character.CHARACTER_TAG.CHARACTER_PLAYER:
                    if (_gridInfo[gridIndex].characterTag == CHARACTER_TAG.CHARACTER_ENEMY ||
                        _gridInfo[gridIndex].characterTag == CHARACTER_TAG.CHARACTER_OTHER)
                    {
                        Methods.SetBitFlag( ref _gridInfo[departIndex].flag, BitFlag.TARGET_ATTACK_BASE);
                    }
                    break;
                case Character.CHARACTER_TAG.CHARACTER_ENEMY:
                    if (_gridInfo[gridIndex].characterTag == CHARACTER_TAG.CHARACTER_PLAYER ||
                        _gridInfo[gridIndex].characterTag == CHARACTER_TAG.CHARACTER_OTHER)
                    {
                        Methods.SetBitFlag(ref _gridInfo[departIndex].flag, BitFlag.TARGET_ATTACK_BASE);
                    }
                    break;
                case Character.CHARACTER_TAG.CHARACTER_OTHER:
                    if (_gridInfo[gridIndex].characterTag == CHARACTER_TAG.CHARACTER_PLAYER ||
                        _gridInfo[gridIndex].characterTag == CHARACTER_TAG.CHARACTER_ENEMY)
                    {
                        Methods.SetBitFlag(ref _gridInfo[departIndex].flag, BitFlag.TARGET_ATTACK_BASE);
                    }
                    break;
                default:
                    break;
            }
        }

        // ���̒l�ł���ΏI��
        if ( --attackRange < 0 ) return;

        // ���[�����O
        if (gridIndex % _gridNumX != 0)
            RegistAttackableEachGrid(gridIndex - 1, attackRange, selfTag, departIndex);       // gridIndex����X��������-1
        // �E�[�����O
        if ((gridIndex + 1) % _gridNumX != 0)
            RegistAttackableEachGrid(gridIndex + 1, attackRange, selfTag, departIndex);       // gridIndex����X��������+1
        // Z�������ւ̉��Z�ƌ��Z�͂��̂܂�
        RegistAttackableEachGrid(gridIndex - _gridNumX, attackRange, selfTag, departIndex);   // gridIndex����Z��������-1
        RegistAttackableEachGrid(gridIndex + _gridNumX, attackRange, selfTag, departIndex);   // gridindex����Z��������+1
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
    /// �O���b�h�����X�V���܂�
    /// </summary>
    public void UpdateGridInfo()
    {
        var btlMgr = BattleManager.Instance;

        // ��x�S�ẴO���b�h������ɖ߂�
        ResetGridInfo();
        // �L�����N�^�[�����݂���O���b�h�̏����X�V
        foreach (Player player in btlMgr.GetPlayerEnumerable())
        {
            var gridIndex               = player.tmpParam.gridIndex;
            ref var info                = ref _gridInfo[gridIndex];
            info.characterTag           = CHARACTER_TAG.CHARACTER_PLAYER;
            info.charaIndex             = player.param.characterIndex;
            Methods.SetBitFlag(ref info.flag, BitFlag.PLAYER_EXIST);
        }
        foreach (Enemy enemy in btlMgr.GetEnemyEnumerable())
        {
            var gridIndex               = enemy.tmpParam.gridIndex;
            ref var info                = ref _gridInfo[gridIndex];
            info.characterTag           = CHARACTER_TAG.CHARACTER_ENEMY;
            info.charaIndex             = enemy.param.characterIndex;
            Methods.SetBitFlag(ref info.flag, BitFlag.ENEMY_EXIST);
        }
    }

    /// <summary>
    /// ���݂̃O���b�h���L�[���͂ő��삵�܂�
    /// </summary>
    public void OperateCurrentGrid()
    {
        // �U���t�F�[�Y��Ԃł͍U���\�ȃL�����N�^�[�����E�őI������
        if (BattleManager.Instance.IsAttackPhaseState())
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow))    { _currentGrid.TransitPrevTarget(); }
            if (Input.GetKeyDown(KeyCode.RightArrow))   { _currentGrid.TransitNextTarget(); }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))      { _currentGrid.Up(); }
            if (Input.GetKeyDown(KeyCode.DownArrow))    { _currentGrid.Down(); }
            if (Input.GetKeyDown(KeyCode.LeftArrow))    { _currentGrid.Left(); }
            if (Input.GetKeyDown(KeyCode.RightArrow))   { _currentGrid.Right(); }
        }
    }

    /// <summary>
    /// �I���O���b�h���w��̃L�����N�^�[�̃O���b�h�ɍ��킹�܂�
    /// </summary>
    /// <param name="character">�w��L�����N�^�[</param>
    public void ApplyCurrentGrid2CharacterGrid( Character character )
    {
        _currentGrid.SetIndex( character.tmpParam.gridIndex );
    }

    /// <summary>
    /// �U���\�O���b�h�̂����A�U���\�L�����N�^�[�����݂���O���b�h�����X�g�ɓo�^���܂�
    /// </summary>
    /// <param name="targetTag">�U���Ώۂ̃^�O</param>
    /// <returns>�U���\�L�����N�^�[�����݂��Ă���</returns>
    public bool RegistAttackTargetGridIndexs(Character.CHARACTER_TAG targetTag, Character target = null)
    {
        Character character = null;
        var btlInstance = BattleManager.Instance;

        _currentGrid.ClearAtkTargetInfo();
        _attackableGridIndexs.Clear();

        // �U���\�A���U���ΏۂƂȂ�L�����N�^�[�����݂���O���b�h�����X�g�ɓo�^
        for (int i = 0; i < _gridInfo.Length; ++i)
        {
            var info = _gridInfo[i];
            if (Methods.CheckBitFlag(info.flag, BitFlag.ATTACKABLE))
            {
                character = btlInstance.GetCharacterFromHashtable(info.characterTag, info.charaIndex);
                if (character != null && character.param.characterTag == targetTag)
                {
                    _attackableGridIndexs.Add(i);
                }
            }
        }

        // �I���O���b�h�������I�ɍU���\�L�����N�^�[�̑��݂���O���b�h�C���f�b�N�X�ɐݒ�
        if (0 < _attackableGridIndexs.Count)
        {
            _currentGrid.SetAtkTargetNum(_attackableGridIndexs.Count);

            // �U���Ώۂ����Ɍ��܂��Ă���ꍇ�͑Ώۂ�T��
            if (target != null && 1 < _attackableGridIndexs.Count)
            {
                for( int i = 0; i < _attackableGridIndexs.Count; ++i )
                {
                    var info = GetGridInfo(_attackableGridIndexs[i]);
                    Character chara = BattleManager.Instance.GetCharacterFromHashtable(info.characterTag, info.charaIndex);
                    if( target == chara )
                    {
                        _currentGrid.SetAtkTargetIndex(i);
                        break;
                    }
                }
            }
            else
            {
                _currentGrid.SetAtkTargetIndex(0);
            }
        }

        return 0 < _attackableGridIndexs.Count;
    }

    /// <summary>
    /// �O���b�h�Ɉړ��\����o�^���܂�
    /// </summary>
    /// <param name="departIndex">�ړ��L�����N�^�[�����݂���O���b�h�̃C���f�b�N�X�l</param>
    /// <param name="moveRange">�ړ��\�͈͒l</param>
    /// <param name="attackRange">�U���\�͈͒l</param>
    /// <param name="selfTag">�L�����N�^�[�^�O</param>
    /// <param name="isAttackable">�U���\���ۂ�</param>
    public void RegistMoveableInfo(int departIndex, int moveRange, int attackRange, Character.CHARACTER_TAG selfTag, bool isAttackable)
    {
        Debug.Assert(0 <= departIndex && departIndex < GridTotalNum, "StageGrid : Irregular Index.");

        // �ړ��ۏ����e�O���b�h�ɓo�^
        RegistMoveableEachGrid(departIndex, moveRange, attackRange, selfTag, isAttackable, true);

        // �o���n�_�ւ͈ړ��s��
        _gridInfo[departIndex].estimatedMoveRange = int.MinValue;
        Methods.UnsetBitFlag(ref _gridInfo[departIndex].flag, BitFlag.ATTACKABLE);
    }

    /// <summary>
    /// �O���b�h�ɍU���\����o�^���܂�
    /// </summary>
    /// <param name="departIndex">�U���L�����N�^�[�����݂���O���b�h�̃C���f�b�N�X�l</param>
    /// <param name="attackRange">�U���\�͈͒l</param>
    public void RegistAttackAbleInfo(int departIndex, int attackRange, Character.CHARACTER_TAG selfTag)
    {
        Debug.Assert(0 <= departIndex && departIndex < GridTotalNum, "StageGrid : Irregular Index.");

        // �S�ẴO���b�h�̍U���ۏ���������
        for (int i = 0; i < GridTotalNum; ++i)
        {
            Methods.UnsetBitFlag(ref _gridInfo[i].flag, BitFlag.ATTACKABLE);
            Methods.UnsetBitFlag(ref _gridInfo[i].flag, BitFlag.TARGET_ATTACK_BASE);
        }

        // �U���ۏ����e�O���b�h�ɓo�^
        RegistAttackableEachGrid(departIndex, attackRange, selfTag, departIndex);
    }

    /// <summary>
    /// �ړ��\�O���b�h��`�悵�܂�
    /// </summary>
    /// <param name="departIndex">�ړ��L�����N�^�[�����݂���O���b�h�̃C���f�b�N�X�l</param>
    /// <param name="moveableRange">�ړ��\�͈͒l</param>
    /// <param name="attackableRange">�U���\�͈͒l</param>
    public void DrawMoveableGrids(int departIndex, int moveableRange, int attackableRange)
    {
        Debug.Assert( 0 <= departIndex && departIndex < GridTotalNum, "StageGrid : Irregular Index.");

        int count = 0;
        // �O���b�h�̏�Ԃ����b�V���ŕ`��
        for (int i = 0; i < GridTotalNum; ++i)
        {
            if (Methods.CheckBitFlag(_gridInfo[i].flag, BitFlag.TARGET_ATTACK_BASE))
            {
                Instantiate(m_GridMeshObject);  // TODO : ��
                _gridMeshs[count++].DrawGridMesh(_gridInfo[i].charaStandPos, gridSize, GridMesh.MeshType.TARGET_ATTACK_BASE);

                continue;
            }

            if (0 <= _gridInfo[i].estimatedMoveRange)
            {
                Instantiate(m_GridMeshObject);  // TODO : ��
                _gridMeshs[count++].DrawGridMesh(_gridInfo[i].charaStandPos, gridSize, GridMesh.MeshType.MOVE);

                Debug.Log("Moveable Grid Index : " + i);
                continue;
            }

            if (Methods.CheckBitFlag(_gridInfo[i].flag, BitFlag.ATTACKABLE))
            {
                Instantiate(m_GridMeshObject);  // TODO : ��
                _gridMeshs[count++].DrawGridMesh(_gridInfo[i].charaStandPos, gridSize, GridMesh.MeshType.ATTACK);

                Debug.Log("Attackable Grid Index : " + i);
                continue;
            }
        }
    }

    /// <summary>
    /// �U���\�O���b�h��`�悵�܂�
    /// </summary>
    /// <param name="departIndex">�U���L�����N�^�[�����݂���O���b�h�̃C���f�b�N�X�l</param>
    public void DrawAttackableGrids(int departIndex)
    {
        Debug.Assert(0 <= departIndex && departIndex < GridTotalNum, "StageGrid : Irregular Index.");

        int count = 0;
        // �O���b�h�̏�Ԃ����b�V���ŕ`��
        for (int i = 0; i < GridTotalNum; ++i)
        {
            if (Methods.CheckBitFlag(_gridInfo[i].flag, BitFlag.ATTACKABLE))
            {
                Instantiate(m_GridMeshObject);  // TODO : ��
                _gridMeshs[count++].DrawGridMesh(_gridInfo[i].charaStandPos, gridSize, GridMesh.MeshType.ATTACK);

                Debug.Log("Attackable Grid Index : " + i);
            }
        }
    }

    /// <summary>
    /// �S�ẴO���b�h�ɂ�����w��̃r�b�g�t���O�̐ݒ���������܂�
    /// </summary>
    public void UnsetGridsBitFlag( BitFlag value )
    {
        // �S�ẴO���b�h�̈ړ��E�U���ۏ���������
        for (int i = 0; i < GridTotalNum; ++i)
        {
            Methods.UnsetBitFlag(ref _gridInfo[i].flag, value);
        }
    }

    /// <summary>
    /// �S�ẴO���b�h���b�V���̕`����������܂�
    /// </summary>
    public void ClearGridMeshDraw()
    {
        foreach( var grid in _gridMeshs )
        {
            grid.ClearDraw();
            grid.Remove();
        }
        _gridMeshs.Clear();
    }

    public void AddGridMeshToList( GridMesh script )
    {
        _gridMeshs.Add( script );
    }

    /// <summary>
    /// �c���Ɖ����̃O���b�h�����擾���܂�
    /// </summary>
    /// <returns>�c���Ɖ����̃O���b�h��</returns>
    public ( int, int ) GetGridNumsXZ()
    {
        return ( _gridNumX, _gridNumZ );
    }

    /// <summary>
    /// �w��O���b�h�ɂ�����L�����N�^�[�̃��[���h���W���擾���܂�
    /// </summary>
    /// <param name="index">�w��O���b�h</param>
    /// <returns>�O���b�h�ɂ����钆�S���[���h���W</returns>
    public Vector3 GetGridCharaStandPos( int index )
    {
        return _gridInfo[index].charaStandPos;
    }

    /// <summary>
    /// ���݂̑I���O���b�h�̃C���f�b�N�X�l���擾���܂�
    /// </summary>
    /// <returns>���݂̑I���O���b�h�̃C���f�b�N�X�l</returns>
    public int GetCurrentGridIndex()
    {
        return _currentGrid.GetIndex();
    }

    /// <summary>
    /// ���ݑI�����Ă���O���b�h�̏����擾���܂�
    /// �U���ΏۑI����Ԃł͑I�����Ă���U���Ώۂ����݂���O���b�h�����擾���܂�
    /// </summary>
    /// <param name="gridInfo">�Y������O���b�h�̏��</param>
    public void FetchCurrentGridInfo( out GridInfo gridInfo )
    {
        int index = 0;

        if(BattleManager.Instance.IsAttackPhaseState())
        {
            index = _attackableGridIndexs[_currentGrid.GetAtkTargetIndex()];
        }
        else
        {
            index = _currentGrid.GetIndex();
        }

        gridInfo = _gridInfo[ index ];
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
    public List<(int routeIndexs, int routeCost)> ExtractShortestRouteIndexs(int departGridIndex, int destGridIndex, in List<int> candidateRouteIndexs)
    {
        Dijkstra dijkstra = new Dijkstra(candidateRouteIndexs.Count);

        // �o���O���b�h����̃C���f�b�N�X�̍����擾
        for ( int i = 0; i + 1 < candidateRouteIndexs.Count; ++i )
        {
            for( int j = i + 1; j < candidateRouteIndexs.Count; ++j )
            {
                int diff = candidateRouteIndexs[j] - candidateRouteIndexs[i];
                if ( (diff == -1 && (candidateRouteIndexs[i] % _gridNumX != 0) ) ||           // ���ɑ���(���[������)
                     (diff == 1 && (candidateRouteIndexs[i] % _gridNumX != _gridNumX - 1)) || // �E�ɑ���(�E�[������)
                      Math.Abs(diff) == _gridNumX)                                            // ��܂��͉��ɑ���
                {
                    // �ړ��\�ȗאڃO���b�h�����_�C�N�X�g���ɓ����
                    dijkstra.Add(i, j);
                    dijkstra.Add(j, i);
                }
            }
        }

        // �_�C�N�X�g������o���O���b�h����ړI�O���b�h�܂ł̍ŒZ�o�H�𓾂�
        return dijkstra.GetMinRoute(candidateRouteIndexs.IndexOf(departGridIndex), candidateRouteIndexs.IndexOf(destGridIndex), candidateRouteIndexs);
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
        _currentGrid.SetIndex(_footprint.gridIndex);
        character.tmpParam.gridIndex = _footprint.gridIndex;
        GridInfo info;
        FetchCurrentGridInfo(out info);
        character.transform.position = info.charaStandPos;
        character.transform.rotation = _footprint.rotation;
    }

    /// <summary>
    /// �w�肳�ꂽ�C���f�b�N�X�Ԃ̃O���b�h����Ԃ��܂�
    /// </summary>
    /// <param name="fromIndex">�n�_�C���f�b�N�X</param>
    /// <param name="toIndex">�I�_�C���f�b�N�X</param>
    /// <returns>�O���b�h��</returns>
    public float CalcurateGridLength( int fromIndex, int toIndex )
    {
        var from        = _gridInfo[fromIndex].charaStandPos;
        var to          = _gridInfo[toIndex].charaStandPos;
        var gridLength  = (from - to).magnitude / gridSize;

        return gridLength;
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
