using Frontier.Entities;
using Frontier.Stage;
using System;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.Stage
{
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

        [Inject] private HierarchyBuilderBase _hierarchyBld = null;

        private TileBehaviour _tileBhv          = null;
        private TileMesh _tileMesh              = null;
        private TileInformation _tileInfo       = null; // 現在のタイル情報
        private TileInformation _tileInfoBase   = null; // 初期化状態のタイル情報
        private TileInformation _tileInfoHold   = null; // 一時保存用のタイル情報

        public TileType Type => _tileType;
        public float Height => _height;

        public void Init( int x, int y, float height, TileType type, GameObject[] prefabs )
        {
            _tileType       = type;
            _height         = height;
            _tileBhv        = CreateTileBehaviour( x, y, _height, _tileType, prefabs );
            _tileMesh       = CreateTileMesh( _tileBhv );
            _tileInfo       = CreateTileInfo( x, y );
            _tileInfoBase   = CreateTileInfo( x, y );
            _tileInfoHold   = CreateTileInfo( x, y );
        }

        public void Dispose()
        {
            if ( _tileBhv != null )
            {
                _tileBhv.Dispose();
                _tileBhv = null;
            }
            if ( _tileMesh != null )
            {
                _tileMesh.Dispose();
                _tileMesh = null;
            }
            if ( _tileInfo != null )
            {
                _tileInfo = null;
            }
            if ( _tileInfoBase != null )
            {
                _tileInfoBase = null;
            }
            if ( _tileInfoHold != null )
            {
                _tileInfoHold = null;
            }
        }

        public StageTileData( TileType type = TileType.None, int height = 0 )
        {
            _tileType = type;
            _height = height;
        }

        /// <summary>
        /// 複製したクラスを生成します
        /// </summary>
        /// <param name="rowIndex"></param>
        /// <param name="colIndex"></param>
        /// <param name="prefabs"></param>
        /// <returns></returns>
        public StageTileData Clone( int colIndex, int rowIndex, GameObject[] prefabs )
        {
            var retData = _hierarchyBld.InstantiateWithDiContainer<StageTileData>( false );
            retData.Init( colIndex, rowIndex, this.Height, this.Type, prefabs );

            return retData;
        }

        public TileInformation CreateTileInfo( int x, int y )
        {
            TileInformation retTileInfo = _hierarchyBld.InstantiateWithDiContainer<TileInformation>( false );
            if ( retTileInfo == null )
            {
                Debug.LogError( "TileInfoのインスタンス化に失敗しました。" );
                return null;
            }

            retTileInfo.Init();

            float charaPosCorrext = 0.5f * TILE_SIZE;       // グリッド位置からキャラの立ち位置への補正値
            float posX = x * TILE_SIZE + charaPosCorrext;
            float posZ = y * TILE_SIZE + charaPosCorrext;
            retTileInfo.charaStandPos = new Vector3( posX, Height, posZ );  // 上記の値から各グリッドのキャラの立ち位置を決定

            return retTileInfo;
        }

        public TileBehaviour CreateTileBehaviour( int x, int y, float height, TileType tileType, GameObject[] prefabs )
        {
            TileBehaviour retTileBhv = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<TileBehaviour>(prefabs[0], true, false, $"Tile_X{x}_Y{y}");
            if ( retTileBhv == null )
            {
                Debug.LogError( "TileBehaviourのインスタンス化に失敗しました。" );
                return null;
            }

            Vector3 position = new Vector3(
            x * TILE_SIZE + 0.5f * TILE_SIZE,   // X座標はグリッドの中心に配置
            0.5f * Height - TILE_MIN_THICKNESS, // Y座標はタイルの高さ(タイルの厚みの最小値は減算する)
            y * TILE_SIZE + 0.5f * TILE_SIZE    // Z座標はグリッドの中心に配置
        );

            retTileBhv.Init( x, y, height, tileType );

            return retTileBhv;
        }

        public TileMesh CreateTileMesh( TileBehaviour tileBhv )
        {
            TileMesh retTileMesh = _hierarchyBld.CreateComponentNestedParentWithDiContainer<TileMesh>(tileBhv.gameObject, true, false, "TileMesh");
            if ( retTileMesh == null )
            {
                Debug.LogError( "TileMeshのインスタンス化に失敗しました。" );
                return null;
            }

            Vector3 meshPos         = tileBhv.transform.position;
            float tileHalfHeight    = 0.5f * tileBhv.transform.localScale.y;    // タイルの高さの半分をY座標に加算して、タイルの中心に配置
            retTileMesh.Init( true, meshPos, tileHalfHeight );
            retTileMesh.DrawMesh();

            return retTileMesh;
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

        public void SetTileTypeAndHeight( TileType type, float height )
        {
            _tileType = type;
            _height = height;
        }

        public Vector3 GetTileScale()
        {
            if ( _tileBhv == null )
            {
                return Vector3.zero;
            }

            return _tileBhv.transform.localScale;
        }

        public ref TileInformation GetTileInfo()
        {
            return ref _tileInfo;
        }
    }
}