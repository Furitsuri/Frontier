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
        // キャラクターの立ち位置座標
        public Vector3 charaStandPos;
        // 移動の可否
        public bool isMoveable;
        // 攻撃の可否
        public bool isAttackable;
        // グリッド上に存在するキャラのインデックス
        public int charaIndex;
        // 初期化
        // TODO : C# 10.0 からは引数なしコンストラクタで定義可能(2023.5時点の最新Unityバージョンでは使用できない)
        public void Init()
        {
            charaStandPos = Vector3.zero;
            isMoveable = false;
            isAttackable = false;
            charaIndex = -1;
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
        _gridTotalNum    = _gridNumX * _gridNumZ;
        _gridInfo        = new GridInfo[_gridTotalNum];

        for (int i = 0; i < _gridTotalNum; ++i)
        {
            // 初期化
            _gridInfo[i].Init();
            // グリッド位置からキャラの立ち位置への補正値
            float charaPosCorrext = 0.5f * gridSize;
            // 1次元配列でデータを扱うため, 横(X軸)方向は剰余で考慮する
            float posX = -_widthX + i % _gridNumX * gridSize + charaPosCorrext;
            // 1次元配列でデータを扱うため, 縦(Z軸)方向は商で考慮する
            float posZ = -_widthZ + i / _gridNumX * gridSize + charaPosCorrext;
            // 上記値から各グリッドのキャラの立ち位置を決定
            _gridInfo[i].charaStandPos = new Vector3(posX, 0, posZ);
        }
    }

    /// <summary>
    /// 移動可能なグリッドを登録します
    /// </summary>
    /// <param name="gridIndex">登録対象のグリッドインデックス</param>
    /// <param name="moveableRange">移動可能範囲値</param>
    void RegistMoveableGrid(int gridIndex, int moveableRange, int attackableRange)
    {
        // 負の値になれば終了
        if (moveableRange < 0) return;
        // 範囲外のグリッドは考慮しない
        if (gridIndex < 0 || _gridTotalNum <= gridIndex) return;

        _gridInfo[gridIndex].isMoveable = true;

        // 移動範囲の端の箇所で攻撃レンジを展開して、攻撃範囲についても登録する
        if (moveableRange == 0)
        {
            RegistAttackableGrid(gridIndex, attackableRange, attackableRange);

            return;
        }

        // 左端を除外
        if ( gridIndex%_gridNumX != 0 )
            RegistMoveableGrid(gridIndex - 1, moveableRange - 1, attackableRange);      // gridIndexからX軸方向へ-1
        // 右端を除外
        if( ( gridIndex + 1 )%_gridNumX != 0 )
            RegistMoveableGrid(gridIndex + 1, moveableRange - 1, attackableRange);      // gridIndexからX軸方向へ+1
        // Z軸方向への加算と減算はそのまま
        RegistMoveableGrid(gridIndex - _gridNumX, moveableRange - 1, attackableRange);  // gridIndexからZ軸方向へ-1
        RegistMoveableGrid(gridIndex + _gridNumX, moveableRange - 1, attackableRange);  // gridIndexからZ軸方向へ+1
    }

    /// <summary>
    /// 攻撃可能なグリッドを登録します
    /// </summary>
    /// <param name="gridIndex">登録対象のグリッドインデックス</param>
    /// <param name="atkRangeMin">攻撃可能範囲の最小値</param>
    /// <param name="atkRangeMax">攻撃可能範囲の最大値</param>
    void RegistAttackableGrid(int gridIndex, int atkRangeMin, int atkRangeMax)
    {
        // 負の値になれば終了
        if (atkRangeMax < 0) return;
        // 範囲外のグリッドは考慮しない
        if (gridIndex < 0 || _gridTotalNum <= gridIndex) return;
        // 攻撃最小レンジの値が1未満の状態のグリッドのみ攻撃可能
        if( atkRangeMin < 1 )
        {
            _gridInfo[gridIndex].isAttackable = true;
        }

        // 左端を除外
        if (gridIndex % _gridNumX != 0)
            RegistAttackableGrid(gridIndex - 1, atkRangeMin - 1, atkRangeMax - 1);    // gridIndexからX軸方向へ-1
        // 右端を除外
        if ((gridIndex + 1) % _gridNumX != 0)
            RegistAttackableGrid(gridIndex + 1, atkRangeMin - 1, atkRangeMax - 1);    // gridIndexからX軸方向へ+1
        // Z軸方向への加算と減算はそのまま
        RegistAttackableGrid(gridIndex - _gridNumX, atkRangeMin - 1, atkRangeMax - 1); // gridIndexからZ軸方向へ-1
        RegistAttackableGrid(gridIndex + _gridNumX, atkRangeMin - 1, atkRangeMax - 1); // indexからZ軸方向へ+1
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
    /// 現在のグリッドをキー入力で操作します
    /// </summary>
    public void OperateCurrentGrid()
    {
        // 攻撃フェーズ状態では攻撃可能なキャラクターを左右で選択する
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

    // 各セル状態の描画
    /// <summary>
    /// 移動可能グリッドを描画します
    /// </summary>
    /// <param name="departIndex">移動キャラクターが存在するグリッドのインデックス値</param>
    /// <param name="moveableRange">移動可能範囲値</param>
    /// <param name="attackableRange">攻撃可能範囲値</param>
    public void DrawMoveableGrids(int departIndex, int moveableRange, int attackableRange)
    {
        if (departIndex < 0 || _gridTotalNum <= departIndex)
        {
            Debug.Assert(false, "StageGrid : Irregular Index.");
            departIndex = 0;
        }

        // 全てのグリッドの移動可否情報を初期化
        for (int i = 0; i < _gridTotalNum; ++i)
        {
            _gridInfo[i].isMoveable = false;
        }

        // 移動可否情報を各グリッドに登録
        RegistMoveableGrid(departIndex, moveableRange, attackableRange);
        // 中心グリッドを除く
        _gridInfo[departIndex].isMoveable = false;
        _gridInfo[departIndex].isAttackable = false;

        int count = 0;
        // グリッドの状態をメッシュで描画
        for (int i = 0; i < _gridTotalNum; ++i)
        {
            if (_gridInfo[i].isMoveable)
            {
                Instantiate(m_GridMeshObject);  // 仮
                _gridMeshs[count++].DrawGridMesh(ref _gridInfo[i].charaStandPos, gridSize, GridMesh.MeshType.MOVE);

                Debug.Log("Moveable Grid Index : " + i);
            }

            if (!_gridInfo[i].isMoveable && _gridInfo[i].isAttackable)
            {
                Instantiate(m_GridMeshObject);  // 仮
                _gridMeshs[count++].DrawGridMesh(ref _gridInfo[i].charaStandPos, gridSize, GridMesh.MeshType.ATTACK);

                Debug.Log("Attackable Grid Index : " + i);
            }
        }
    }

    /// <summary>
    /// 攻撃可能グリッドを描画します
    /// </summary>
    /// <param name="departIndex">攻撃キャラクターが存在するグリッドのインデックス値</param>
    /// <param name="attackableRangeMin">攻撃可能範囲の最小値</param>
    /// <param name="attackableRangeMax">攻撃可能範囲の最大値</param>
    public void DrawAttackableGrids(int departIndex, int attackableRangeMin, int attackableRangeMax)
    {
        if (departIndex < 0 || _gridTotalNum <= departIndex)
        {
            Debug.Assert(false, "StageGrid : Irregular Index.");
            departIndex = 0;
        }

        // 全てのグリッドの攻撃可否情報を初期化
        for (int i = 0; i < _gridTotalNum; ++i)
        {
            _gridInfo[i].isAttackable = false;
        }

        // 移動可否情報を各グリッドに登録
        RegistAttackableGrid(departIndex, attackableRangeMin, attackableRangeMax);

        int count = 0;
        // グリッドの状態をメッシュで描画
        for (int i = 0; i < _gridTotalNum; ++i)
        {
            if (_gridInfo[i].isAttackable)
            {
                Instantiate(m_GridMeshObject);  // TODO : 仮
                _gridMeshs[count++].DrawGridMesh(ref _gridInfo[i].charaStandPos, gridSize, GridMesh.MeshType.ATTACK);

                Debug.Log("Attackable Grid Index : " + i);
            }
        }
    }

    /// <summary>
    /// 全てのグリッドの可否情報を初期化し、描画を消去します
    /// </summary>
    public void ClearGridsCondition()
    {
        // 全てのグリッドの移動・攻撃可否情報を初期化
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
    /// 現在選択しているグリッドの情報を取得します
    /// 攻撃対象選択状態では選択している攻撃対象が存在するグリッド情報を取得します
    /// </summary>
    /// <returns>該当するグリッドの情報</returns>
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
    public List<int> ExtractDepart2DestGoalGridIndexs(int departGridIndex, int destGridIndex)
    {
        List<int> pathIndexs = new List<int>(64);

        // スタート地点をパス情報に追加する
        pathIndexs.Add(departGridIndex);

        // 移動可能グリッドのみ抜き出す
        for ( int i = 0; i < _gridInfo.Length; ++i )
        {
            if (_gridInfo[i].isMoveable )
            {
                pathIndexs.Add(i);
            }
        }

        Dijkstra dijkstra = new Dijkstra(pathIndexs.Count);

        // 出発グリッドからのインデックスの差を取得
        for ( int i = 0; i + 1 < pathIndexs.Count; ++i )
        {
            for( int j = i + 1; j < pathIndexs.Count; ++j )
            {
                int diff = pathIndexs[j] - pathIndexs[i];
                if ( (diff == -1 && (pathIndexs[i] % _gridNumX != 0) ) ||           // 左に存在(左端を除く)
                     (diff == 1 && (pathIndexs[i] % _gridNumX != _gridNumX - 1)) ||  // 右に存在(右端を除く)
                      Math.Abs(diff) == _gridNumX)                                  // 上または下に存在
                {
                    // 移動可能な隣接グリッド情報をダイクストラに入れる
                    dijkstra.Add(i, j);
                }
            }
        }

        // ダイクストラから出発グリッドから目的グリッドまでの最短経路を得る
        List<int> minRouteIndexs = dijkstra.GetMinRoute(pathIndexs.IndexOf(departGridIndex), pathIndexs.IndexOf(destGridIndex));
        for( int i = 0; i < minRouteIndexs.Count; ++i )
        {
            minRouteIndexs[i] = pathIndexs[ minRouteIndexs[i] ];
        }
        
        return minRouteIndexs;
    }

    /// <summary>
    /// 攻撃可能グリッドのうち、攻撃可能キャラクターが存在するグリッドをリストに登録します
    /// </summary>
    /// <param name="targetTag">攻撃対象のタグ</param>
    /// <returns>攻撃可能キャラクターが存在している</returns>
    public bool RegistAttackTargetGridIndexs( Character.CHARACTER_TAG targetTag )
    {
        Character character = null;
        var btlInstance = BattleManager.Instance;

        currentGrid.ClearAtkTargetInfo();
        _attackableGridIndexs.Clear();

        // 攻撃可能、かつ攻撃対象となるキャラクターが存在するグリッドをリストに登録
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

        // 選択グリッドを自動的に攻撃可能キャラクターの存在するグリッドインデックスに設定
        if( 0 < _attackableGridIndexs.Count )
        {
            currentGrid.SetAtkTargetNum( _attackableGridIndexs.Count );
            currentGrid.SetAtkTargetIndex(0);
        }

        return 0 < _attackableGridIndexs.Count;
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
