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
    /// グリッドに対するフラグ情報
    /// </summary>
   public enum BitFlag
    {
        NONE = 0,
        CANNOT_MOVE         = 1 << 0,   // 移動不可
        ATTACKABLE          = 1 << 1,   // 攻撃可能
        TARGET_ATTACK_BASE  = 1 << 2,   // ターゲット攻撃候補地点
        PLAYER_EXIST        = 1 << 3,   // プレイヤーキャラクターが存在
        ENEMY_EXIST         = 1 << 4,   // 敵キャラクターが存在
        OTHER_EXIST         = 1 << 5,   // 第三勢力が存在
    }

    public struct GridInfo
    {
        // キャラクターの立ち位置座標(※)
        public Vector3 charaStandPos;
        // 移動阻害値(※)
        public int moveResist;
        // 移動値の見積もり値
        public int estimatedMoveRange;
        // グリッド上に存在するキャラクターのタイプ
        public Character.CHARACTER_TAG characterTag;
        // グリッド上に存在するキャラクターのインデックス
        public int charaIndex;
        // フラグ情報
        public BitFlag flag;
        // ※ 一度設定された後は変更することがない変数

        /// <summary>
        /// 初期化します
        /// TODO：ステージのファイル読込によってmoveRangeをはじめとした値を設定出来るようにしたい
        ///       また、C# 10.0 からは引数なしコンストラクタで定義可能(2023.5時点の最新Unityバージョンでは使用できない)
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

    // キャラクターの位置を元に戻す際に使用します
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
        // バトルマネージャに登録
        BattleManager.Instance.registStageGrid(this);

        _gridMeshs          = new List<GridMesh>();
        _attackableGridIndexs  = new List<int>();

        // ステージ情報から各サイズを参照する
        if (isAdjustStageScale)
        {
            _gridNumX = (int)(Math.Floor(m_StageObject.GetComponent<Renderer>().bounds.size.x) / gridSize);
            _gridNumZ = (int)(Math.Floor(m_StageObject.GetComponent<Renderer>().bounds.size.z) / gridSize);
        }

        // メッシュを描画
        GetComponent<MeshFilter>().mesh = _mesh = new Mesh();
        _mesh = ReGrid(_mesh);

        // グリッド情報の初期化
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

        // X方向の頂点
        for (int i = 0; count < 2 * (_gridNumX + 1); ++i, count = 2 * i)
        {
            vertices[count]     = new Vector3(startPosition.x + ((float)i * gridSize), startPosition.y, 0);
            vertices[count + 1] = new Vector3(startPosition.x + ((float)i * gridSize), endPosition.y, 0);
        }
        // Y(Z)方向の頂点
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
    /// グリッド情報を初期化します
    /// </summary>
    void InitGridInfo()
    {
        GridTotalNum        = _gridNumX * _gridNumZ;
        _gridInfo           = new GridInfo[GridTotalNum];
        _gridInfoBase       = new GridInfo[GridTotalNum]; ;

        for (int i = 0; i < GridTotalNum; ++i)
        {
            // 初期化
            _gridInfo[i].Init();
            _gridInfoBase[i].Init();
            // グリッド位置からキャラの立ち位置への補正値
            float charaPosCorrext = 0.5f * gridSize;
            // 1次元配列でデータを扱うため, 横(X軸)方向は剰余で考慮する
            float posX = -_widthX + i % _gridNumX * gridSize + charaPosCorrext;
            // 1次元配列でデータを扱うため, 縦(Z軸)方向は商で考慮する
            float posZ = -_widthZ + i / _gridNumX * gridSize + charaPosCorrext;
            // 上記値から各グリッドのキャラの立ち位置を決定
            _gridInfoBase[i].charaStandPos = _gridInfo[i].charaStandPos = new Vector3(posX, 0, posZ);
            // TODO : ファイル読み込みから通行不能な箇所などのBitFlag情報を設定出来るようにする
        }
    }

    /// <summary>
    /// _gridInfoの状態を基の状態に戻します
    /// </summary>
    void ResetGridInfo()
    {
        for (int i = 0; i < GridTotalNum; ++i)
        {
            _gridInfo[i] = _gridInfoBase[i];
        }
    }

    /// <summary>
    /// 移動可能なグリッドを登録します
    /// </summary>
    /// <param name="gridIndex">登録対象のグリッドインデックス</param>
    /// <param name="moveRange">移動可能範囲値</param>
    /// <param name="attackRange">攻撃可能範囲値</param>
    /// <param name="selfTag">呼び出し元キャラクターのキャラクタータグ</param>
    /// <param name="isAttackable">呼び出し元のキャラクターが攻撃可能か否か</param>
    /// <param name="isDeparture">出発グリッドから呼び出されたか否か</param>
    void RegistMoveableEachGrid(int gridIndex, int moveRange, int attackRange, Character.CHARACTER_TAG selfTag, bool isAttackable, bool isDeparture = false)
    {
        // 範囲外のグリッドは考慮しない
        if (gridIndex < 0 || GridTotalNum <= gridIndex) return;
        // 移動不可のグリッドに辿り着いた場合は終了
        if (Methods.CheckBitFlag(_gridInfo[gridIndex].flag, BitFlag.CANNOT_MOVE)) return;
        // 既に計算済みのグリッドであれば終了
        if (moveRange <= _gridInfo[gridIndex].estimatedMoveRange) return;
        // 自身に対する敵対勢力キャラクターが存在すれば終了
        StageGrid.BitFlag[] opponentTag = new StageGrid.BitFlag[(int)CHARACTER_TAG.CHARACTER_NUM]
        {
            BitFlag.ENEMY_EXIST  | BitFlag.OTHER_EXIST,     // PLAYERにおける敵対勢力
            BitFlag.PLAYER_EXIST | BitFlag.OTHER_EXIST,     // ENEMYにおける敵対勢力
            BitFlag.PLAYER_EXIST | BitFlag.ENEMY_EXIST      // OTHERにおける敵対勢力
        };
        if (Methods.CheckBitFlag(_gridInfo[gridIndex].flag, opponentTag[(int)selfTag])) return;

        // 現在グリッドの移動抵抗値を更新( 出発グリッドではmoveRangeの値をそのまま適応する )
        int currentMoveRange = (isDeparture) ? moveRange : _gridInfo[gridIndex].moveResist + moveRange;
        _gridInfo[gridIndex].estimatedMoveRange = currentMoveRange;

        // 負の値であれば終了
        if (currentMoveRange < 0) return;

        // 攻撃範囲についても登録する
        if (isAttackable && _gridInfo[gridIndex].characterTag == CHARACTER_TAG.CHARACTER_NONE)
            RegistAttackableEachGrid(gridIndex, attackRange, selfTag, gridIndex);
        // 左端を除外
        if ( gridIndex%_gridNumX != 0 )
            RegistMoveableEachGrid(gridIndex - 1, currentMoveRange, attackRange, selfTag, isAttackable);      // gridIndexからX軸方向へ-1
        // 右端を除外
        if( ( gridIndex + 1 )%_gridNumX != 0 )
            RegistMoveableEachGrid(gridIndex + 1, currentMoveRange, attackRange, selfTag, isAttackable);      // gridIndexからX軸方向へ+1
        // Z軸方向への加算と減算はそのまま
        RegistMoveableEachGrid(gridIndex - _gridNumX, currentMoveRange, attackRange, selfTag, isAttackable);  // gridIndexからZ軸方向へ-1
        RegistMoveableEachGrid(gridIndex + _gridNumX, currentMoveRange, attackRange, selfTag, isAttackable);  // gridIndexからZ軸方向へ+1
    }

    /// <summary>
    /// 攻撃可能なグリッドを登録します
    /// </summary>
    /// <param name="gridIndex">対象のグリッドインデックス</param>
    /// <param name="attackRange">攻撃可能範囲値</param>
    /// <param name="selfTag">自身のキャラクタータグ</param>
    /// <param name="departIndex">出発グリッドインデックス</param>
    void RegistAttackableEachGrid(int gridIndex, int attackRange, Character.CHARACTER_TAG selfTag, int departIndex)
    {
        // 範囲外のグリッドは考慮しない
        if (gridIndex < 0 || GridTotalNum <= gridIndex) return;
        // 移動不可のグリッドには攻撃できない
        if (Methods.CheckBitFlag(_gridInfo[gridIndex].flag, BitFlag.CANNOT_MOVE)) return;
        // 出発地点でなければ登録
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

        // 負の値であれば終了
        if ( --attackRange < 0 ) return;

        // 左端を除外
        if (gridIndex % _gridNumX != 0)
            RegistAttackableEachGrid(gridIndex - 1, attackRange, selfTag, departIndex);       // gridIndexからX軸方向へ-1
        // 右端を除外
        if ((gridIndex + 1) % _gridNumX != 0)
            RegistAttackableEachGrid(gridIndex + 1, attackRange, selfTag, departIndex);       // gridIndexからX軸方向へ+1
        // Z軸方向への加算と減算はそのまま
        RegistAttackableEachGrid(gridIndex - _gridNumX, attackRange, selfTag, departIndex);   // gridIndexからZ軸方向へ-1
        RegistAttackableEachGrid(gridIndex + _gridNumX, attackRange, selfTag, departIndex);   // gridindexからZ軸方向へ+1
    }

    /// <summary>
    /// 頂点配列データをすべて指定の方向へ回転移動させます
    /// </summary>
    /// <param name="vertices">回転させる頂点配列データ</param>
    /// <param name="rotDirection">回転方向</param>
    /// <returns>回転させた頂点配列データ</returns>
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
    /// グリッド情報を更新します
    /// </summary>
    public void UpdateGridInfo()
    {
        var btlMgr = BattleManager.Instance;

        // 一度全てのグリッド情報を基に戻す
        ResetGridInfo();
        // キャラクターが存在するグリッドの情報を更新
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
    /// 現在のグリッドをキー入力で操作します
    /// </summary>
    public void OperateCurrentGrid()
    {
        // 攻撃フェーズ状態では攻撃可能なキャラクターを左右で選択する
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
    /// 選択グリッドを指定のキャラクターのグリッドに合わせます
    /// </summary>
    /// <param name="character">指定キャラクター</param>
    public void ApplyCurrentGrid2CharacterGrid( Character character )
    {
        _currentGrid.SetIndex( character.tmpParam.gridIndex );
    }

    /// <summary>
    /// 攻撃可能グリッドのうち、攻撃可能キャラクターが存在するグリッドをリストに登録します
    /// </summary>
    /// <param name="targetTag">攻撃対象のタグ</param>
    /// <returns>攻撃可能キャラクターが存在している</returns>
    public bool RegistAttackTargetGridIndexs(Character.CHARACTER_TAG targetTag, Character target = null)
    {
        Character character = null;
        var btlInstance = BattleManager.Instance;

        _currentGrid.ClearAtkTargetInfo();
        _attackableGridIndexs.Clear();

        // 攻撃可能、かつ攻撃対象となるキャラクターが存在するグリッドをリストに登録
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

        // 選択グリッドを自動的に攻撃可能キャラクターの存在するグリッドインデックスに設定
        if (0 < _attackableGridIndexs.Count)
        {
            _currentGrid.SetAtkTargetNum(_attackableGridIndexs.Count);

            // 攻撃対象が既に決まっている場合は対象を探す
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
    /// グリッドに移動可能情報を登録します
    /// </summary>
    /// <param name="departIndex">移動キャラクターが存在するグリッドのインデックス値</param>
    /// <param name="moveRange">移動可能範囲値</param>
    /// <param name="attackRange">攻撃可能範囲値</param>
    /// <param name="selfTag">キャラクタータグ</param>
    /// <param name="isAttackable">攻撃可能か否か</param>
    public void RegistMoveableInfo(int departIndex, int moveRange, int attackRange, Character.CHARACTER_TAG selfTag, bool isAttackable)
    {
        Debug.Assert(0 <= departIndex && departIndex < GridTotalNum, "StageGrid : Irregular Index.");

        // 移動可否情報を各グリッドに登録
        RegistMoveableEachGrid(departIndex, moveRange, attackRange, selfTag, isAttackable, true);

        // 出発地点へは移動不可に
        _gridInfo[departIndex].estimatedMoveRange = int.MinValue;
        Methods.UnsetBitFlag(ref _gridInfo[departIndex].flag, BitFlag.ATTACKABLE);
    }

    /// <summary>
    /// グリッドに攻撃可能情報を登録します
    /// </summary>
    /// <param name="departIndex">攻撃キャラクターが存在するグリッドのインデックス値</param>
    /// <param name="attackRange">攻撃可能範囲値</param>
    public void RegistAttackAbleInfo(int departIndex, int attackRange, Character.CHARACTER_TAG selfTag)
    {
        Debug.Assert(0 <= departIndex && departIndex < GridTotalNum, "StageGrid : Irregular Index.");

        // 全てのグリッドの攻撃可否情報を初期化
        for (int i = 0; i < GridTotalNum; ++i)
        {
            Methods.UnsetBitFlag(ref _gridInfo[i].flag, BitFlag.ATTACKABLE);
            Methods.UnsetBitFlag(ref _gridInfo[i].flag, BitFlag.TARGET_ATTACK_BASE);
        }

        // 攻撃可否情報を各グリッドに登録
        RegistAttackableEachGrid(departIndex, attackRange, selfTag, departIndex);
    }

    /// <summary>
    /// 移動可能グリッドを描画します
    /// </summary>
    /// <param name="departIndex">移動キャラクターが存在するグリッドのインデックス値</param>
    /// <param name="moveableRange">移動可能範囲値</param>
    /// <param name="attackableRange">攻撃可能範囲値</param>
    public void DrawMoveableGrids(int departIndex, int moveableRange, int attackableRange)
    {
        Debug.Assert( 0 <= departIndex && departIndex < GridTotalNum, "StageGrid : Irregular Index.");

        int count = 0;
        // グリッドの状態をメッシュで描画
        for (int i = 0; i < GridTotalNum; ++i)
        {
            if (Methods.CheckBitFlag(_gridInfo[i].flag, BitFlag.TARGET_ATTACK_BASE))
            {
                Instantiate(m_GridMeshObject);  // TODO : 仮
                _gridMeshs[count++].DrawGridMesh(_gridInfo[i].charaStandPos, gridSize, GridMesh.MeshType.TARGET_ATTACK_BASE);

                continue;
            }

            if (0 <= _gridInfo[i].estimatedMoveRange)
            {
                Instantiate(m_GridMeshObject);  // TODO : 仮
                _gridMeshs[count++].DrawGridMesh(_gridInfo[i].charaStandPos, gridSize, GridMesh.MeshType.MOVE);

                Debug.Log("Moveable Grid Index : " + i);
                continue;
            }

            if (Methods.CheckBitFlag(_gridInfo[i].flag, BitFlag.ATTACKABLE))
            {
                Instantiate(m_GridMeshObject);  // TODO : 仮
                _gridMeshs[count++].DrawGridMesh(_gridInfo[i].charaStandPos, gridSize, GridMesh.MeshType.ATTACK);

                Debug.Log("Attackable Grid Index : " + i);
                continue;
            }
        }
    }

    /// <summary>
    /// 攻撃可能グリッドを描画します
    /// </summary>
    /// <param name="departIndex">攻撃キャラクターが存在するグリッドのインデックス値</param>
    public void DrawAttackableGrids(int departIndex)
    {
        Debug.Assert(0 <= departIndex && departIndex < GridTotalNum, "StageGrid : Irregular Index.");

        int count = 0;
        // グリッドの状態をメッシュで描画
        for (int i = 0; i < GridTotalNum; ++i)
        {
            if (Methods.CheckBitFlag(_gridInfo[i].flag, BitFlag.ATTACKABLE))
            {
                Instantiate(m_GridMeshObject);  // TODO : 仮
                _gridMeshs[count++].DrawGridMesh(_gridInfo[i].charaStandPos, gridSize, GridMesh.MeshType.ATTACK);

                Debug.Log("Attackable Grid Index : " + i);
            }
        }
    }

    /// <summary>
    /// 全てのグリッドにおける指定のビットフラグの設定を解除します
    /// </summary>
    public void UnsetGridsBitFlag( BitFlag value )
    {
        // 全てのグリッドの移動・攻撃可否情報を初期化
        for (int i = 0; i < GridTotalNum; ++i)
        {
            Methods.UnsetBitFlag(ref _gridInfo[i].flag, value);
        }
    }

    /// <summary>
    /// 全てのグリッドメッシュの描画を消去します
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
    /// 縦軸と横軸のグリッド数を取得します
    /// </summary>
    /// <returns>縦軸と横軸のグリッド数</returns>
    public ( int, int ) GetGridNumsXZ()
    {
        return ( _gridNumX, _gridNumZ );
    }

    /// <summary>
    /// 指定グリッドにおけるキャラクターのワールド座標を取得します
    /// </summary>
    /// <param name="index">指定グリッド</param>
    /// <returns>グリッドにおける中心ワールド座標</returns>
    public Vector3 GetGridCharaStandPos( int index )
    {
        return _gridInfo[index].charaStandPos;
    }

    /// <summary>
    /// 現在の選択グリッドのインデックス値を取得します
    /// </summary>
    /// <returns>現在の選択グリッドのインデックス値</returns>
    public int GetCurrentGridIndex()
    {
        return _currentGrid.GetIndex();
    }

    /// <summary>
    /// 現在選択しているグリッドの情報を取得します
    /// 攻撃対象選択状態では選択している攻撃対象が存在するグリッド情報を取得します
    /// </summary>
    /// <param name="gridInfo">該当するグリッドの情報</param>
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
    /// 指定インデックスのグリッド情報を取得します
    /// </summary>
    /// <param name="index">指定するインデックス値</param>
    /// <returns>指定インデックスのグリッド情報</returns>
    public ref GridInfo GetGridInfo( int index )
    {
        return ref _gridInfo[index];
    }

    /// <summary>
    /// 出発地点と目的地から移動経路となるグリッドのインデックスリストを取得します
    /// </summary>
    /// <param name="departGridIndex">出発地グリッドのインデックス</param>
    /// <param name="destGridIndex">目的地グリッドのインデックス</param>
    public List<(int routeIndexs, int routeCost)> ExtractShortestRouteIndexs(int departGridIndex, int destGridIndex, in List<int> candidateRouteIndexs)
    {
        Dijkstra dijkstra = new Dijkstra(candidateRouteIndexs.Count);

        // 出発グリッドからのインデックスの差を取得
        for ( int i = 0; i + 1 < candidateRouteIndexs.Count; ++i )
        {
            for( int j = i + 1; j < candidateRouteIndexs.Count; ++j )
            {
                int diff = candidateRouteIndexs[j] - candidateRouteIndexs[i];
                if ( (diff == -1 && (candidateRouteIndexs[i] % _gridNumX != 0) ) ||           // 左に存在(左端を除く)
                     (diff == 1 && (candidateRouteIndexs[i] % _gridNumX != _gridNumX - 1)) || // 右に存在(右端を除く)
                      Math.Abs(diff) == _gridNumX)                                            // 上または下に存在
                {
                    // 移動可能な隣接グリッド情報をダイクストラに入れる
                    dijkstra.Add(i, j);
                    dijkstra.Add(j, i);
                }
            }
        }

        // ダイクストラから出発グリッドから目的グリッドまでの最短経路を得る
        return dijkstra.GetMinRoute(candidateRouteIndexs.IndexOf(departGridIndex), candidateRouteIndexs.IndexOf(destGridIndex), candidateRouteIndexs);
    }

    /// <summary>
    /// キャラクターの位置及び向きを保持します
    /// </summary>
    /// <param name="footprint">保持する値</param>
    public void LeaveFootprint( Footprint footprint )
    {
        _footprint = footprint;
    }

    /// <summary>
    /// 保持していた位置及び向きを指定のキャラクターに設定します
    /// </summary>
    /// <param name="character">指定するキャラクター</param>
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
    /// 指定されたインデックス間のグリッド長を返します
    /// </summary>
    /// <param name="fromIndex">始点インデックス</param>
    /// <param name="toIndex">終点インデックス</param>
    /// <returns>グリッド長</returns>
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

            // ステージ情報からサイズを決める際はサイズ編集を不可にする
            EditorGUI.BeginDisabledGroup(script.isAdjustStageScale);
            script._gridNumX = EditorGUILayout.IntField("X方向グリッド数", script._gridNumX);
            script._gridNumZ = EditorGUILayout.IntField("Z方向グリッド数", script._gridNumZ);
            EditorGUI.EndDisabledGroup();

            base.OnInspectorGUI();
        }
    }
#endif // UNITY_EDITOR
}
