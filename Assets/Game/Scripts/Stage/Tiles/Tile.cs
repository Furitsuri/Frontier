using Frontier.Stage;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Pool;
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
        private List<TileMesh> _tileMeshes;
        private MeshRenderer _renderer;

        public TileStaticData StaticData() => _tileStaticData;
        public TileDynamicData DynamicData() => _tileDynamicData;

        private void Awake()
        {
            if( null == _gridLineMesh )
            {
                _gridLineMesh = CreateGridLineMesh( this );
                NullCheck.AssertNotNull( _gridLineMesh, "_gridLineMesh" );
            }
            if( null == _tileStaticData )
            {
                _tileStaticData = _hierarchyBld.InstantiateWithDiContainer<TileStaticData>( false );
                NullCheck.AssertNotNull( _tileStaticData, "_tileStaticData" );
            }
            if( null == _tileDynamicData )
            {
                _tileDynamicData = _hierarchyBld.InstantiateWithDiContainer<TileDynamicData>( false );
                NullCheck.AssertNotNull( _tileDynamicData, "_tileDynamicData" );
            }
            if( null == _baseDynamicData )
            {
                _baseDynamicData = _hierarchyBld.InstantiateWithDiContainer<TileDynamicData>( false );
            }

            _tileMeshes             = UnityEngine.Pool.ListPool<TileMesh>.Get();
            _renderer               = GetComponent<MeshRenderer>();
            transform.localScale    = TileMaterialLibrary.GetDefaultTileScale();   // タイルのデフォルトスケールを設定
        }

        public void Init( int x, int y, float height, TileType type )
        {
            _tileStaticData.Init( x, y, height, type );
            _tileDynamicData.Init();
            _baseDynamicData.Init();
            _tileMeshes.Clear();

            Vector3 position = new Vector3(
                x * TILE_SIZE + 0.5f * TILE_SIZE,   // X座標はグリッドの中心に配置
                0.5f * height - TILE_MIN_THICKNESS, // Y座標はタイルの高さ(タイルの厚みの最小値は減算する)
                y * TILE_SIZE + 0.5f * TILE_SIZE    // Z座標はグリッドの中心に配置
            );

            transform.position      = position;
            transform.localScale    = new Vector3( TILE_SIZE, height + TILE_MIN_THICKNESS, TILE_SIZE );
            transform.rotation      = Quaternion.identity;
            _renderer.material      = TileMaterialLibrary.GetMaterial( type );
        }

        public void Dispose()
        {
            _gridLineMesh?.Dispose();
            ClearTileMeshes();
            UnityEngine.Pool.ListPool<TileMesh>.Release( _tileMeshes );
            _tileMeshes = null;

            if( _renderer != null )
            {
                Destroy( _renderer );
            }

            Destroy( gameObject );
            Destroy( this );
        }

        /// <summary>
        /// 初期状態のタイル情報を、現在のタイル情報に適応させます
        /// </summary>
        public void ApplyBaseTileDynamicData()
        {
            _baseDynamicData.CopyTo( _tileDynamicData );
        }

        public void DrawTileMesh( TileMesh tileMesh, Color color )
        {
            tileMesh.DrawTileMesh( transform.position, ADD_TILE_POS_Y * ( _tileMeshes.Count + 1 ), TILE_SIZE, color );
            _tileMeshes.Add( tileMesh );
        }

        public void ClearTileMeshes()
        {
            foreach( var tile in _tileMeshes )
            {
                tile.ClearDraw();
                tile.Remove();
            }
            _tileMeshes.Clear();
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

            Vector3 meshPos = tileBhv.transform.position;
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
            ret.Init( colIndex, rowIndex, this._tileStaticData.Height, this._tileStaticData.TileType );

            return ret;
        }
    }
}