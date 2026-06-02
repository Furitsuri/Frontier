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

        // CharacterKey をキーとして各キャラクター固有の TileMesh を一意に管理します
        private Dictionary<CharacterKey, TileMesh> _tileMeshes = new Dictionary<CharacterKey, TileMesh>();

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

            if( _renderer != null )
            {
                Destroy( _renderer );
            }

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
        /// 指定オーナーキーのタイルメッシュを削除します。
        /// 削除後は残りのタイルメッシュの Y オフセットを再設定します。
        /// </summary>
        public void ClearTileMesh( CharacterKey ownerKey )
        {
            if( !_tileMeshes.TryGetValue( ownerKey, out var tileMesh ) ) { return; }

            tileMesh.ClearDraw();
            tileMesh.Remove();
            _tileMeshes.Remove( ownerKey );

            ReDrawTileMeshes(); // 残りのタイルメッシュの Y 座標を再設定
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

        /// <summary>
        /// 指定オーナーキーに対応するタイルメッシュを返します。存在しない場合は null を返します。
        /// </summary>
        public TileMesh GetTileMeshByOwnerKey( CharacterKey ownerKey )
        {
            _tileMeshes.TryGetValue( ownerKey, out var tileMesh );
            return tileMesh;
        }

        /// <summary>
        /// タイルメッシュを描画します。
        /// 同一キャラクターの既存メッシュが存在する場合は置き換えます（一キャラクター一メッシュを保証）。
        /// </summary>
        public void DrawTileMesh( TileMesh tileMesh, in Color color, CharacterKey ownerKey )
        {
            if( _tileMeshes.TryGetValue( ownerKey, out var existing ) )
            {
                // 既存メッシュの挿入順インデックスを保持したまま置き換える
                int idx = GetEntryIndex( ownerKey );
                existing.ClearDraw();
                existing.Remove();
                _tileMeshes[ownerKey] = tileMesh;
                tileMesh.DrawTileMesh( transform.position, ADD_TILE_POS_Y * ( idx + 1 ), in color );
            }
            else
            {
                // 新規追加: 現在の件数から Y オフセットを決定してから辞書に挿入
                float yOffset = GetTileMeshPosYOffset();
                _tileMeshes[ownerKey] = tileMesh;
                tileMesh.DrawTileMesh( transform.position, yOffset, in color );
            }
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
        public Tile Clone( int colIndex, int rowIndex, GameObject[] prefabs )
        {
            var ret = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<Tile>( prefabs[0], true, false, $"Tile_X{colIndex}_Y{rowIndex}" );
            ret.Init( colIndex, rowIndex, this._tileStaticData.IsDeployable, this._tileStaticData.Height, this._tileStaticData.TileType );

            return ret;
        }

        /// <summary>
        /// 保持しているデータをタイルのセーブデータに加工して返します
        /// </summary>
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
        /// 残存するタイルメッシュの Y 座標を挿入順に再設定して再描画します
        /// </summary>
        private void ReDrawTileMeshes()
        {
            int i = 0;
            foreach( var entry in _tileMeshes )
            {
                entry.Value.ClearDraw();
                entry.Value.DrawTileMesh( transform.position, ADD_TILE_POS_Y * ( i + 1 ), entry.Value.GetColor() );
                ++i;
            }
        }

        /// <summary>
        /// 指定キーの辞書内挿入順インデックスを返します
        /// </summary>
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
