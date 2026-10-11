using Frontier.Entities;
using Frontier.Stage;
using System;
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
        private TileProfile _profile                = null; // タイプごとの見た目・挙動プロファイル

        /// <summary>
        /// タイルメッシュを重ねる順番(下から上)を決める比較子です。
        /// 追加・削除の履歴によって重なり方が変わらないよう、CharacterKeyのみから順番を決めます。
        /// プレイヤーの範囲表示が最も上になるよう、陣営はOTHER→ENEMY→PLAYERの順、同じ陣営内はインデックスの昇順とします。
        /// </summary>
        private class TileMeshStackOrderComparer : IComparer<CharacterKey>
        {
            public int Compare( CharacterKey x, CharacterKey y )
            {
                int tagOrder = ( ( int ) y.CharacterTag ).CompareTo( ( int ) x.CharacterTag );
                if( tagOrder != 0 ) { return tagOrder; }

                return x.CharacterIndex.CompareTo( y.CharacterIndex );
            }
        }

        // CharacterKey をキーとして各キャラクターの行動範囲を示すTileMeshを一意に管理します
        // (列挙順がそのまま重ねる順番になるよう、TileMeshStackOrderComparerで整列させています)
        private SortedDictionary<CharacterKey, TileMesh> _tileMeshes        = new SortedDictionary<CharacterKey, TileMesh>( new TileMeshStackOrderComparer() );
        /// <summary>
        /// 危険範囲(敵・第三勢力が攻撃可能な範囲)の表示を、陣営ごとに1枚のTileMeshとして管理するための層です。
        /// 同じ陣営の複数のキャラクターの危険範囲が重なっても濃くならないよう、キャラクターごとには重ねません。
        /// </summary>
        private class DangerRangeLayer
        {
            public TileMesh Mesh                            = null;
            public Color Color                              = Color.clear;
            public readonly HashSet<CharacterKey> OwnerKeys = new HashSet<CharacterKey>();   // このタイルを危険範囲に含んでいるキャラクター
        }

        // 危険範囲の層を重ねる順番(下から上)。敵の危険範囲を優先して上に表示し、第三勢力はその下に表示します
        private static readonly CHARACTER_TAG[] DangerRangeLayerOrder = { CHARACTER_TAG.OTHER, CHARACTER_TAG.ENEMY };

        // 陣営をキーとして危険範囲の層を管理します。各キャラクターの行動範囲を示すTileMesh(_tileMeshes)よりも下に表示します
        private Dictionary<CHARACTER_TAG, DangerRangeLayer> _dangerRangeLayers = new Dictionary<CHARACTER_TAG, DangerRangeLayer>();
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
            _dangerRangeLayers.Clear();

            _profile = TileMaterialLibrary.GetProfile( type );

            // 見た目の沈み量。底面は他タイルと揃えたまま「上面だけ」を下げる（＝背を低くする）。
            // 厚みを超えて沈めると形状が反転するため、高さの範囲内にクランプする。
            float visualDrop = Mathf.Min( _profile.VisualHeightOffset, height );

            Vector3 position = new Vector3(
                x * TILE_SIZE + 0.5f * TILE_SIZE,                           // X座標はグリッドの中心に配置
                0.5f * height - TILE_MIN_THICKNESS - 0.5f * visualDrop,     // Y座標(底面は他タイルと揃え、上面のみ visualDrop 分下げるため中心を半分だけ下げる)
                y * TILE_SIZE + 0.5f * TILE_SIZE                            // Z座標はグリッドの中心に配置
            );

            transform.position   = position;
            // 上面を下げた分だけ厚みを縮める → 底面位置は他タイルと同じまま（下に出っ張らない）
            transform.localScale = new Vector3( TILE_SIZE, height + TILE_MIN_THICKNESS - visualDrop, TILE_SIZE );
            transform.rotation   = Quaternion.identity;
            _renderer.material   = _profile.Material;

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

        /// <summary>このタイルが側面カリング（同種隣接面の非表示）を使用するかを返します。</summary>
        public bool UsesSideFaceCulling => _profile != null && _profile.UseSideFaceCulling;

        /// <summary>
        /// 側面表示マスクをマテリアルに適用します。
        /// dirs の各成分 (x=+X右, y=-X左, z=+Z前, w=-Z後) は
        /// 1=側面を表示 / 0=側面を非表示（同種が隣接しシームレスにする）を表します。
        /// 側面カリングを使用しないタイプには何もしません。
        /// </summary>
        public void ApplySideFaceMask( Vector4 dirs )
        {
            if( !UsesSideFaceCulling ) { return; }

            _renderer.material.SetVector( "_SideAlphaDirs", dirs );
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

            foreach( var layer in _dangerRangeLayers.Values )
            {
                layer.Mesh.ClearDraw();
                layer.Mesh.Remove();
            }
            _dangerRangeLayers.Clear();
        }

        /// <summary>
        /// 指定オーナーキーのキャラクターの危険範囲として、このタイルを登録します。
        /// 危険範囲は陣営ごとに1枚のタイルメッシュで表示され、同じ陣営のキャラクターが既に登録している場合は見た目が変わりません。
        /// </summary>
        /// <param name="ownerKey">危険範囲の持主</param>
        /// <param name="color">危険範囲の色</param>
        /// <param name="tileMeshFactory">その陣営の危険範囲を初めて表示する際に用いる、タイルメッシュの生成処理</param>
        public void AddDangerRange( CharacterKey ownerKey, in Color color, Func<TileMesh> tileMeshFactory )
        {
            if( !_dangerRangeLayers.TryGetValue( ownerKey.CharacterTag, out var layer ) )
            {
                layer = new DangerRangeLayer { Mesh = tileMeshFactory(), Color = color };
                layer.Mesh.MapType = TileMapType.ATTACKABLE;
                _dangerRangeLayers[ownerKey.CharacterTag] = layer;
                layer.OwnerKeys.Add( ownerKey );

                // 層が増えたことで、それより上に表示するメッシュの段がずれるため配置し直す
                ReDrawTileMeshes();

                return;
            }

            layer.OwnerKeys.Add( ownerKey );
        }

        /// <summary>
        /// 指定オーナーキーのキャラクターの危険範囲から、このタイルを除外します。
        /// 同じ陣営の他のキャラクターがこのタイルを危険範囲に含んでいる間は、表示は残ります。
        /// </summary>
        public void RemoveDangerRange( CharacterKey ownerKey )
        {
            if( !_dangerRangeLayers.TryGetValue( ownerKey.CharacterTag, out var layer ) ) { return; }

            layer.OwnerKeys.Remove( ownerKey );
            if( 0 < layer.OwnerKeys.Count ) { return; }

            layer.Mesh.ClearDraw();
            layer.Mesh.Remove();
            _dangerRangeLayers.Remove( ownerKey.CharacterTag );

            ReDrawTileMeshes();
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
            bool isReplaced = _tileMeshes.TryGetValue( ownerKey, out var existing );
            if( isReplaced )
            {
                existing.ClearDraw();
                existing.Remove();
            }

            _tileMeshes[ownerKey]   = tileMesh;
            tileMesh.MapType        = mapType;

            // 危険範囲の層は、各キャラクターの行動範囲を示すメッシュよりも下に表示する
            int slotIndex = _dangerRangeLayers.Count;
            foreach( var entry in _tileMeshes )
            {
                if( ReferenceEquals( entry.Value, tileMesh ) )
                {
                    tileMesh.DrawTileMesh( _tileStaticData.CursorStandPos, GetTileMeshDrawYOffset( slotIndex ), in color );
                }
                // 置き換えの場合は他のメッシュの段は変わらないため、新規に追加した場合のみ他のメッシュを配置し直す
                else if( !isReplaced )
                {
                    entry.Value.ClearDraw();
                    entry.Value.DrawTileMesh( _tileStaticData.CursorStandPos, GetTileMeshDrawYOffset( slotIndex ), entry.Value.GetColor() );
                }

                ++slotIndex;
            }
        }

        private float GetTileMeshDrawYOffset( int slotIndex )
        {
            return ADD_TILE_POS_Y * ( slotIndex + 1 );
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
        /// タイルメッシュを再描画します。
        /// 下から順に、危険範囲の層(陣営ごとに1枚)、各キャラクターの行動範囲を示すメッシュを重ねて配置します。
        /// </summary>
        private void ReDrawTileMeshes()
        {
            int i = 0;

            foreach( CHARACTER_TAG tag in DangerRangeLayerOrder )
            {
                if( !_dangerRangeLayers.TryGetValue( tag, out var layer ) ) { continue; }

                layer.Mesh.ClearDraw();
                layer.Mesh.DrawTileMesh( _tileStaticData.CursorStandPos, GetTileMeshDrawYOffset( i ), in layer.Color );
                ++i;
            }

            foreach( var entry in _tileMeshes )
            {
                entry.Value.ClearDraw();
                entry.Value.DrawTileMesh( _tileStaticData.CursorStandPos, GetTileMeshDrawYOffset( i ), entry.Value.GetColor() );
                // MapType は DrawTileMesh でリセットされないため保持される
                ++i;
            }
        }
    }
}
