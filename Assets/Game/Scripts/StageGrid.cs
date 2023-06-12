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
public class StageGrid : MonoBehaviour
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

    public enum Face
    {
        xy,
        zx,
        yz,
    };

    public static StageGrid instance = null;

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
    private float widthX, widthZ;
    private Mesh mesh;
    private GridInfo[] gridInfo;
    private List<GridMesh> gridMeshs;
    private List<int> attackRangeIndexs;
    private bool isAttackTargetSelect = false;

    public CurrentGrid currentGrid { get; private set; } = CurrentGrid.GetInstance();

    void Awake()
    {
        // インスタンスの作成
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }

        DontDestroyOnLoad(gameObject);

        // バトルマネージャに登録
        BattleManager.instance.registStageGrid(this);

        gridMeshs           = new List<GridMesh>();
        attackRangeIndexs  = new List<int>();

        // ステージ情報から各サイズを参照する
        if (isAdjustStageScale)
        {
            _gridNumX = (int)(Math.Floor(m_StageObject.GetComponent<Renderer>().bounds.size.x) / gridSize);
            _gridNumZ = (int)(Math.Floor(m_StageObject.GetComponent<Renderer>().bounds.size.z) / gridSize);
        }

        // メッシュを描画
        GetComponent<MeshFilter>().mesh = mesh = new Mesh();
        mesh = ReGrid(mesh);

        // グリッド情報の初期化
        InitGridInfo();

        currentGrid.Init(0, _gridNumX, _gridNumZ);
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

        widthX                  = gridSize * _gridNumX / 2.0f;
        widthZ                  = gridSize * _gridNumZ / 2.0f;
        Vector2 startPosition   = new Vector2(-widthX, -widthZ);
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

        mesh.vertices = RotationVertices(vertices, rotDirection);
        mesh.uv = uvs;
        mesh.colors = colors;
        mesh.SetIndices(lines, MeshTopology.Lines, 0);

        return mesh;
    }

    // グリッド情報の初期化
    void InitGridInfo()
    {
        _gridTotalNum    = _gridNumX * _gridNumZ;
        gridInfo        = new GridInfo[_gridTotalNum];

        for (int i = 0; i < _gridTotalNum; ++i)
        {
            // 初期化
            gridInfo[i].Init();
            // グリッド位置からキャラの立ち位置への補正値
            float charaPosCorrext = 0.5f * gridSize;
            // 1次元配列でデータを扱うため, 横(X軸)方向は剰余で考慮する
            float posX = -widthX + i % _gridNumX * gridSize + charaPosCorrext;
            // 1次元配列でデータを扱うため, 縦(Z軸)方向は商で考慮する
            float posZ = -widthZ + i / _gridNumX * gridSize + charaPosCorrext;
            // 上記値から各グリッドのキャラの立ち位置を決定
            gridInfo[i].charaStandPos = new Vector3(posX, 0, posZ);
        }
    }

    void RegistMoveableGrid(int gridIndex, int moveRange)
    {
        // 負の値になれば終了
        if (moveRange < 0) return;
        // 範囲外のグリッドは考慮しない
        if (gridIndex < 0 || _gridTotalNum <= gridIndex) return;

        gridInfo[gridIndex].isMoveable = true;

        // 左端を除外
        if( gridIndex%_gridNumX != 0 )
            RegistMoveableGrid(gridIndex - 1, moveRange - 1);    // gridIndexからX軸方向へ-1
        // 右端を除外
        if( ( gridIndex + 1 )%_gridNumX != 0 )
            RegistMoveableGrid(gridIndex + 1, moveRange - 1);    // gridIndexからX軸方向へ+1
        // Z軸方向への加算と減算はそのまま
        RegistMoveableGrid(gridIndex - _gridNumX, moveRange - 1); // gridIndexからZ軸方向へ-1
        RegistMoveableGrid(gridIndex + _gridNumX, moveRange - 1); // gridIndexからZ軸方向へ+1
    }

    void RegistAttackableGrid(int gridIndex, int atkRangeMin, int atkRangeMax)
    {
        // 負の値になれば終了
        if (atkRangeMax < 0) return;
        // 範囲外のグリッドは考慮しない
        if (gridIndex < 0 || _gridTotalNum <= gridIndex) return;
        // 攻撃最小レンジの値が1未満の状態のグリッドのみ攻撃可能
        if( atkRangeMin < 1 )
        {
            gridInfo[gridIndex].isAttackable = true;
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
        if (BattleManager.instance.IsAttackPhaseState())
        {
            // if (Input.GetKeyDown(KeyCode.LeftArrow)) { CurrentGridIndex -= 1; }
            // if (Input.GetKeyDown(KeyCode.RightArrow)) { CurrentGridIndex += 1; }
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
    public void DrawGridsCondition(int departIndex, int moveable, BattleManager.TurnType type)
    {
        if (departIndex < 0 || _gridTotalNum <= departIndex)
        {
            Debug.Assert(false, "StageGrid : Irregular Index.");
            departIndex = 0;
        }

        // 全てのグリッドの移動可否情報を初期化
        for (int i = 0; i < _gridTotalNum; ++i)
        {
            gridInfo[i].isMoveable = false;
        }

        // 移動可否情報を各グリッドに登録
        RegistMoveableGrid(departIndex, moveable);
        // 中心グリッドを除く
        gridInfo[departIndex].isMoveable = false;

        int count = 0;
        // グリッドの状態をメッシュで描画
        for (int i = 0; i < _gridTotalNum; ++i)
        {
            if (gridInfo[i].isMoveable)
            {
                Instantiate(m_GridMeshObject);  // 仮
                gridMeshs[count++].DrawGridMesh(ref gridInfo[i].charaStandPos, gridSize, GridMesh.MeshType.MOVE);

                Debug.Log("Moveable Grid Index : " + i);
            }
        }
    }

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
            gridInfo[i].isAttackable = false;
        }

        // 移動可否情報を各グリッドに登録
        RegistAttackableGrid(departIndex, attackableRangeMin, attackableRangeMax);

        int count = 0;
        // グリッドの状態をメッシュで描画
        for (int i = 0; i < _gridTotalNum; ++i)
        {
            if (gridInfo[i].isAttackable)
            {
                Instantiate(m_GridMeshObject);  // 仮
                gridMeshs[count++].DrawGridMesh(ref gridInfo[i].charaStandPos, gridSize, GridMesh.MeshType.ATTACK);

                Debug.Log("Attackable Grid Index : " + i);
            }
        }
    }

    public void clearGridsCondition()
    {
        // 全てのグリッドの移動可否情報を初期化
        for (int i = 0; i < _gridTotalNum; ++i)
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
        for (int i = 0; i < _gridTotalNum; ++i)
        {
            gridInfo[i].charaIndex = -1;
        }
    }

    public void AddGridMeshToList( GridMesh script )
    {
        gridMeshs.Add( script );
    }

    public void ToggleAttackTargetSelect( bool isTargetSelect )
    {
        isAttackTargetSelect = isTargetSelect;
    }

    public ref Vector3 getGridCharaStandPos( int index )
    {
        return ref gridInfo[index].charaStandPos;
    }

    // 選択中グリッド情報の取得
    public ref GridInfo getCurrentGridInfo()
    {
        return ref gridInfo[ currentGrid.GetIndex() ];
    }

    // 指定グリッド情報の取得
    public ref GridInfo getGridInfo( int index )
    {
        return ref gridInfo[index];
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
        for ( int i = 0; i < gridInfo.Length; ++i )
        {
            if (gridInfo[i].isMoveable )
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
    /// 攻撃可能地点となるグリッドのインデックスを取得します
    /// </summary>
    public void ApplyAttackTargetGridIndexs( int attackerGridIndex )
    {
        Character character = null;
        var btlInstance = BattleManager.instance;

        attackRangeIndexs.Clear();

        // 攻撃可能グリッドのみ抜き出す
        for (int i = 0; i < gridInfo.Length; ++i)
        {
            if (gridInfo[i].isAttackable)
            {
                attackRangeIndexs.Add(i);
            }
        }

        // 攻撃可能グリッド
        for (int i = 0; i < attackRangeIndexs.Count; ++i)
        {
            var info = getGridInfo(attackRangeIndexs[i]);
            character = btlInstance.SearchCharacterFromCharaIndex( info.charaIndex );
            if (character != null && character.param.charaTag == Character.CHARACTER_TAG.CHARACTER_ENEMY)
            {
                currentGrid.SetIndex( attackRangeIndexs[i] );

                break;
            }
        }
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
