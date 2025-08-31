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
    [SerializeField]
    private TileType _tileType;
    [SerializeField]
    private float _height;

    private TileBehaviour _tileBhv  = null;
    private TileMesh _tileMesh      = null;
    private GridInfo _tileInfo      = null; // 現在のタイル情報
    private GridInfo _tileInfoBase  = null; // 初期化状態のタイル情報
    private GridInfo _tileInfoHold  = null; // 一時保存用のタイル情報

    public TileType Type => _tileType;
    public float Height => _height;

    public void Init()
    {
        _tileType        = TileType.None;
        _height          = 0;
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
        if ( _tileInfoHold != null )
        {
            _tileInfoHold = null;
        }
    }

    public StageTileData(TileType type = TileType.None, int height = 0)
    {
        _tileType    = type;
        _height      = height;
    }

    public void InstantiateTileInfo( int index, int rowNum, HierarchyBuilderBase hierarchyBld )
    {
        _tileInfo       = hierarchyBld.InstantiateWithDiContainer<GridInfo>( false );
        _tileInfoBase   = hierarchyBld.InstantiateWithDiContainer<GridInfo>( false );
        _tileInfoHold   = hierarchyBld.InstantiateWithDiContainer<GridInfo>( false );   // _tileInfoHoldは一時保存で使用されるため、Initする必要がない
        if (_tileInfo == null || _tileInfoBase == null || _tileInfoHold == null )
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
        // 上記の値から各グリッドのキャラの立ち位置を決定
        _tileInfoBase.charaStandPos = _tileInfo.charaStandPos = new Vector3(posX, Height, posZ);
    }

    public void InstantiateTileBhv(int x, int y, GameObject[] prefabs, HierarchyBuilderBase hierarchyBld )
    {
        if (_tileBhv != null) return; // 既にインスタンス化されている場合は何もしない

        Vector3 position = new Vector3(
            x * TILE_SIZE + 0.5f * TILE_SIZE,   // X座標はグリッドの中心に配置
            0.5f * Height - TILE_THICKNESS_MIN, // Y座標はタイルの高さ(タイルの厚みの最小値は減算する)
            y * TILE_SIZE + 0.5f * TILE_SIZE    // Z座標はグリッドの中心に配置
        );

        _tileBhv = hierarchyBld.CreateComponentAndOrganizeWithDiContainer<TileBehaviour>(prefabs[0], true, false, $"Tile_X{x}_Y{y}");
        _tileBhv.transform.position     = position;
        _tileBhv.transform.localScale   = new Vector3(TILE_SIZE, Height + TILE_THICKNESS_MIN, TILE_SIZE);
        _tileBhv.transform.rotation     = Quaternion.identity;
        _tileBhv.ApplyTileType(_tileType);
    }

    public void InstantiateTileMesh( HierarchyBuilderBase hierarchyBld )
    {
        if (_tileMesh != null) return; // 既にインスタンス化されている場合は何もしない
        if (_tileBhv == null)
        {
            Debug.LogError("TileBehaviourがインスタンス化されていません。TileMeshの初期化に失敗しました。");
            return;
        }

        _tileMesh = hierarchyBld.CreateComponentNestedParentWithDiContainer<TileMesh>(_tileBhv.gameObject, true, false, "TileMesh");
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

    /// <summary>
    /// 現在のタイル情報を一時保存します
    /// </summary>
    public void HoldCurrentTileInfo()
    {
        _tileInfoHold = _tileInfo.Copy();
    }

    /// <summary>
    /// 初期状態のタイル情報を、現在のタイル情報に適応させます
    /// </summary>
    public void ApplyBaseTileInfo()
    {
        _tileInfo = _tileInfoBase.Copy();
    }

    /// <summary>
    /// 一時保存中のタイル情報を、現在のタイル情報に適応させます
    /// </summary>
    public void ApplyHeldTileInfo()
    {
        _tileInfo = _tileInfoHold.Copy();
    }

    public void SetTileTypeAndHeight(TileType type, float height)
    {
        _tileType    = type;
        _height      = height;
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