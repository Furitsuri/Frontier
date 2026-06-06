using Frontier.Stage;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.Stage
{
    public class Tile : MonoBehaviour, IDisposer
    {
        [Inject] HierarchyBuilderBase _hierarchyBld = null;

        private TileStaticData _tileStaticData      = null;
        private TileDynamicData _tileDynamicData    = null;
        private GridLineMesh _gridLineMesh          = null; // 各タイルの縁を示すグリッド線
        private TileDynamicData _baseDynamicData    = null;
        private MeshRenderer _renderer;

        // CharacterKey をキーとして各キャラクターの行動範囲を示すTileMeshを一意に管理します
        private Dictionary<CharacterKey, TileMesh> _tileMeshes              = new Dictionary<CharacterKey, TileMesh>();
        // CharacterKey をキーとして各キャラクターの移動方向を一意に管理します
        private Dictionary<CharacterKey, MoveDirectionType> _moveDirections = new Dictionary<CharacterKey, MoveDirectionType>();

        public TileStaticData StaticData() => _tileStaticData;
        public TileDynamicData DynamicData() => _tileDynamicData;

        private void Awake()
        {
            LazyInject.GetOrCreate( ref _gridLineMesh, () => CreateGridLineMesh( this ) );
            LazyInject.GetOrCreate( ref _tileStaticData, () => _hierarchyBld.InstantiateWithDiContainer<TileStaticData>( false ) );
            LazyInject.GetOrCreate( ref _tileDynamicData, () => _hierarchyBld.InstantiateWithDiContainer<TileDynamicData>( false ) );
            LazyInject.GetOrCreate( ref _baseDynamicData, () => _hierarchyBld.InstantiateWithDiContainer<TileDynamicData>( false ) );

            _renderer = GetComponent<MeshRenderer>();
            transform.localScale = TileMaterialLibrary.GetDefaultTileScale();   // タイルのデフォルトスケールを設定
        }

        public void Init( int x, int y, bool isDeployable, float height, TileType type )
        {
            _tileStaticData.Init( x, y, isDeployable, height, type );
            _tileDynamicData.Init();
            _baseDynamicData.Init();
            _tileMeshes.Clear();

            Vector3 position = new Vector3(
                x * TILE_SIZE + 0.5f * TILE_SIZE,   // X座標はグリッドの中心に配置
                0.5f * height - TILE_MIN_THICKNESS, // Y座標はタイルの高さ(タイルの厚みの最小値は減算する)
                y * TILE_SIZE + 0.5f * TILE_SIZE    // Z座標はグリッドの中心に配置
            );

            transform.position   = position;
            transform.localScale = new Vector3( TILE_SIZE, height + TILE_MIN_THICKNESS, TILE_SIZE );
            transform.rotation   = Quaternion.identity;
            _renderer.material   = TileMaterialLibrary.GetMaterial( type );

            ApplyDeployableColor();
        }

        public void Dispose()
        {
            _gridLineMesh?.Dispose();
            ClearTileMeshes();

            if( _renderer != null ) { Destroy( _renderer ); }

            Destroy( gameObject );
            Destroy( this );
        }

        /// <summary>
        /// タイルの色で配置可否を示すようにマテリアルの色を変更します
        /// </summary>
        public void ApplyDeployableColor()
        {
            if( StaticData().IsDeployable ) { _renderer.material.color = Color.white; }
            else { _renderer.material.color = new Color( 0.5f, 0.5f, 0.5f, 1f ); }
        }

        /// <summary>
        /// 初期状態のタイル情報を、現在のタイル情報に適応させます
        /// </summary>
        public void ApplyBaseTileDynamicData()
        {
            _baseDynamicData.CopyTo( _tileDynamicData );
        }

        /// <summary>
        /// 指定オーナーキーのタイルメッシュを削除します（タイプ問わず）
        /// </summary>
        public void ClearTileMesh( CharacterKey ownerKey )
        {
            if( !_tileMeshes.TryGetValue( ownerKey, out var tileMesh ) ) { return; }

            tileMesh.ClearDraw();
            tileMesh.Remove();
            _tileMeshes.Remove( ownerKey );

            ReDrawTileMeshes();
        }

        /// <summary>
        /// 指定オーナーキーかつ指定タイプに一致するタイルメッシュを削除します
        /// </summary>
        public void ClearTileMesh( CharacterKey ownerKey, TileMapType types )
        {
            if( !_tileMeshes.TryGetValue( ownerKey, out var tileMesh ) ) { return; }
            if( ( ( int ) tileMesh.MapType & ( int ) types ) == 0 ) { return; }

            tileMesh.ClearDraw();
            tileMesh.Remove();
            _tileMeshes.Remove( ownerKey );

            ReDrawTileMeshes();
        }

        /// <summary>
        /// タイルに登録されているタイルメッシュを全て削除します
        /// </summary>
        public void ClearTileMeshes()
        {
            foreach( var tileMesh in _tileMeshes.Values )
            {
                tileMesh.ClearDraw();
                tileMesh.Remove();
            }
            _tileMeshes.Clear();
        }

        /// <summary>
        /// 配置可否を示すタイルの色を元に戻します
        /// </summary>
        public void ClearUndeployableColor()
        {
            _renderer.material.color = Color.white;
        }

        public TileMesh GetTileMeshByOwnerKey( CharacterKey ownerKey )
        {
            _tileMeshes.TryGetValue( ownerKey, out var tileMesh );
            return tileMesh;
        }

        /// <summary>
        /// タイルメッシュを描画します。同一キャラクターの既存メッシュが存在する場合は置き換えます。
        /// mapType に TileMapType を指定することで TileMesh と種別を紐づけます。
        /// </summary>
        public void DrawTileMesh( TileMesh tileMesh, in Color color, CharacterKey ownerKey, TileMapType mapType )
        {
            if( _tileMeshes.TryGetValue( ownerKey, out var existing ) )
            {
                int idx = GetEntryIndex( ownerKey );
                existing.ClearDraw();
                existing.Remove();
                _tileMeshes[ownerKey] = tileMesh;
                tileMesh.DrawTileMesh( transform.position, ADD_TILE_POS_Y * ( idx + 1 ), in color );
            }
            else
            {
                float yOffset = GetTileMeshPosYOffset();
                _tileMeshes[ownerKey] = tileMesh;
                tileMesh.DrawTileMesh( transform.position, yOffset, in color );
            }
            tileMesh.MapType = mapType;
        }

        public float GetTileMeshPosYOffset()
        {
            return ADD_TILE_POS_Y * ( _tileMeshes.Count + 1 );
        }

        public Vector3 GetScale()
        {
            return transform.localScale;
        }

        /// <summary>
        /// タイルのグリッド線メッシュを生成します
        /// </summary>
        /// <param name="tileBhv"></param>
        /// <returns></returns>
        public GridLineMesh CreateGridLineMesh( Tile tileBhv )
        {
            GridLineMesh retLineMesh = _hierarchyBld.CreateComponentNestedParentWithDiContainer<GridLineMesh>( tileBhv.gameObject, true, false, "TileMesh" );
            if( retLineMesh == null )
            {
                Debug.LogError( "TileMeshのインスタンス化に失敗しました。" );
                return null;
            }

            Vector3 meshPos      = tileBhv.transform.position;
            float tileHalfHeight = 0.5f * tileBhv.transform.localScale.y;    // タイルの高さの半分をY座標に加算して、タイルの中心に配置
            retLineMesh.Init( true, meshPos, tileHalfHeight );
            retLineMesh.DrawMesh();

            return retLineMesh;
        }

        /// <summary>
        /// 複製したクラスを生成します
        /// </summary>
        /// <param name="rowIndex"></param>
        /// <param name="colIndex"></param>
        /// <param name="prefabs"></param>
        /// <returns></returns>
        public Tile Clone( int colIndex, int rowIndex, GameObject[] prefabs )
        {
            var ret = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<Tile>( prefabs[0], true, false, $"Tile_X{colIndex}_Y{rowIndex}" );
            ret.Init( colIndex, rowIndex, this._tileStaticData.IsDeployable, this._tileStaticData.Height, this._tileStaticData.TileType );

            return ret;
        }

        /// <summary>
        /// 保持しているデータをタイルのセーブデータに加工して返します
        /// </summary>
        /// <returns></returns>
        public TileSaveData ToSaveData()
        {
            return new TileSaveData
            {
                IsDeployable    = _tileStaticData.IsDeployable,
                Height          = _tileStaticData.Height,
                TileType        = _tileStaticData.TileType
            };
        }

        /// <summary>
        /// タイルメッシュを再描画します
        /// </summary>
        private void ReDrawTileMeshes()
        {
            int i = 0;
            foreach( var entry in _tileMeshes )
            {
                entry.Value.ClearDraw();
                entry.Value.DrawTileMesh( transform.position, ADD_TILE_POS_Y * ( i + 1 ), entry.Value.GetColor() );
                // MapType は DrawTileMesh でリセットされないため保持される
                ++i;
            }
        }

        private int GetEntryIndex( CharacterKey ownerKey )
        {
            int idx = 0;
            foreach( var key in _tileMeshes.Keys )
            {
                if( key.Equals( ownerKey ) ) { return idx; }
                ++idx;
            }
            return 0;
        }
    }
}
