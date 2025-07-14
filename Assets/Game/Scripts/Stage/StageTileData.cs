using Frontier.Stage;
using UnityEngine;
using Zenject;
using static Constants;

[System.Serializable]
public class StageTileData
{
    /// <summary>
    /// ステージデータとして保存するもののみpublic属性にしています。
    /// </summary>
    public TileType TileType { get; private set; }
    public float Height { get; private set; }

    private HierarchyBuilderBase _hierarchyBld;
    private TileBehaviour _tileBhv  = null;
    private TileMesh _tileMesh      = null;
    private GridInfo _tileInfo      = null;
    private GridInfo _tileInfoBase  = null;

    [Inject]
    public void Construct(HierarchyBuilderBase hierarchyBld)
    {
        _hierarchyBld = hierarchyBld;
    }

    public void Init()
    {
        TileType        = TileType.None;
        Height          = 0;
        _tileBhv        = null;
        _tileMesh       = null;
        _tileInfo       = null;
        _tileInfoBase   = null;
    }

    public void Dispose()
    {
        if (_tileBhv != null)
        {
            _tileBhv.Dispose();
            _tileBhv = null;
        }
        if (_tileMesh != null)
        {
            _tileMesh.Dispose();
            _tileMesh = null;
        }
        if (_tileInfo != null)
        {
            _tileInfo = null;
        }
        if (_tileInfoBase != null)
        {
            _tileInfoBase = null;
        }
    }

    public StageTileData(TileType type = TileType.None, int height = 0)
    {
        TileType    = type;
        Height      = height;
    }

    public void InstantiateTileInfo( int index, int rowNum )
    {
        _tileInfo       = _hierarchyBld.InstantiateWithDiContainer<GridInfo>(false);
        _tileInfoBase   = _hierarchyBld.InstantiateWithDiContainer<GridInfo>(false);
        if (_tileInfo == null || _tileInfoBase == null )
        {
            Debug.LogError("TileInfoのインスタンス化に失敗しました。");
            return;
        }

        _tileInfo.Init();
        _tileInfoBase.Init();
        // グリッド位置からキャラの立ち位置への補正値
        float charaPosCorrext = 0.5f * TILE_SIZE;
        // 1次元配列でデータを扱うため, 横(X軸)方向は剰余で考慮する
        float posX = index % rowNum * TILE_SIZE + charaPosCorrext;
        // 1次元配列でデータを扱うため, 縦(Z軸)方向は商で考慮する
        float posZ = index / rowNum * TILE_SIZE + charaPosCorrext;
        // 上記値から各グリッドのキャラの立ち位置を決定
        _tileInfoBase.charaStandPos = _tileInfo.charaStandPos = new Vector3(posX, Height, posZ);
    }

    public void InstantiateTileBhv(int x, int y, GameObject[] prefabs)
    {
        if (_tileBhv != null) return; // 既にインスタンス化されている場合は何もしない

        Vector3 position = new Vector3(
            x * TILE_SIZE + 0.5f * TILE_SIZE,   // X座標はグリッドの中心に配置
            0.5f * Height - TILE_THICKNESS_MIN, // Y座標はタイルの高さ(タイルの厚みの最小値は減算する)
            y * TILE_SIZE + 0.5f * TILE_SIZE    // Z座標はグリッドの中心に配置
        );

        _tileBhv = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<TileBehaviour>(prefabs[0], true, false, $"Tile_X{x}_Y{y}");
        _tileBhv.transform.position     = position;
        _tileBhv.transform.localScale   = new Vector3(TILE_SIZE, Height + TILE_THICKNESS_MIN, TILE_SIZE);
        _tileBhv.transform.rotation     = Quaternion.identity;
        _tileBhv.ApplyTileType(TileType);
    }

    public void InstantiateTileMesh()
    {
        if (_tileMesh != null) return; // 既にインスタンス化されている場合は何もしない
        if (_tileBhv == null)
        {
            Debug.LogError("TileBehaviourがインスタンス化されていません。TileMeshの初期化に失敗しました。");
            return;
        }

        _tileMesh = _hierarchyBld.CreateComponentNestedParentWithDiContainer<TileMesh>(_tileBhv.gameObject, true, false, "TileMesh");
        if (_tileMesh == null)
        {
            Debug.LogError("TileMeshのインスタンス化に失敗しました。");
            return;
        }

        Vector3 meshPos         = _tileBhv.transform.position;
        float tileHalfHeight    = 0.5f * _tileBhv.transform.localScale.y;    // タイルの高さの半分をY座標に加算して、タイルの中心に配置
        _tileMesh.Init(true, meshPos, tileHalfHeight);
        _tileMesh.DrawMesh();
    }

    public void CopyTileInfoBaseToOriginal()
    {
        _tileInfo = _tileInfoBase.Copy();
    }

    public void SetTileTypeAndHeight(TileType type, float height)
    {
        TileType    = type;
        Height      = height;
    }

    public Vector3 GetTileScale()
    {
        if ( _tileBhv == null )
        {
            return Vector3.zero;
        }

        return _tileBhv.transform.localScale;
    }

    public ref GridInfo GetTileInfo()
    {
        return ref _tileInfo;
    }
}