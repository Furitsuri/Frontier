using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.Stage
{
    /// <summary>
    /// ステージ上のグリッド数などのデータ
    /// [SerializeField]属性のインスタンスが、ステージエディター上で編集対象となるデータです
    /// </summary>
    [System.Serializable]
    public class StageData
    {
        [SerializeField] private int _maxDeployableUnits;                   // ステージに出撃可能なユニット数
        [SerializeField] private int _tileRowNum;                           // タイルの行数
        [SerializeField] private int _tileColNum;                           // タイルの列数
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        [SerializeField] private TileSaveData[] _tileSaveDatas = null;      // タイル内データのうち保存する必要のあるデータ
#endif // DEVELOPMENT_BUILD || UNITY_EDITOR

        [Inject] private HierarchyBuilderBase _hierarchyBld = null;

        private Tile[] _tiles = null;           // タイル

        public int MaxDeployableUnits => _maxDeployableUnits;
        public int TileRowNum => _tileRowNum;
        public int TileColNum => _tileColNum;

        public void Init( int maxDeployableUnits, int tileRowNum, int tileColumnNum )
        {
            _maxDeployableUnits = maxDeployableUnits;
            _tileRowNum         = tileRowNum;
            _tileColNum         = tileColumnNum;
            _tiles              = new Tile[_tileRowNum * _tileColNum];

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            _tileSaveDatas = new TileSaveData[_tileRowNum * _tileColNum];
#endif // DEVELOPMENT_BUILD || UNITY_EDITOR
        }

        public void CreateDefaultTiles( GameObject[] tilePrefabs )
        {
            for( int x = 0; x < _tileColNum; x++ )
            {
                for( int y = 0; y < _tileRowNum; y++ )
                {
                    _tiles[x + ( y * _tileColNum )] = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<Tile>( tilePrefabs[0], true, false, $"Tile_X{x}_Y{y}" );
                    _tiles[x + ( y * _tileColNum )].Init( x, y, false, 0f, TileType.None );
                }
            }
        }

        public void Dispose()
        {
            if( _tiles != null )
            {
                foreach( var tile in _tiles )
                {
                    Methods.Dispose( tile );
                }

                _tiles = null;
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if( _tileSaveDatas != null )
            {
                foreach( var saveData in _tileSaveDatas )
                {
                    Methods.Dispose( saveData );
                }

                _tileSaveDatas = null;
            }
#endif // DEVELOPMENT_BUILD || UNITY_EDITOR
        }

        /// <summary>
        /// 側面カリングを使用するタイル（水など）について、隣接4方向に同種タイルがあるかを調べ、
        /// 同種同士で接している面は非表示（シームレス）、露出している面は表示するよう
        /// 側面マスクをマテリアルへ適用します。
        /// マインクラフトのブロック面カリングと同じ考え方で、
        /// 「隣に何も無いタイルの側面が透けてしまう」問題を解消します。
        /// 全タイルのタイプ確定後（ロード完了後）に一度だけ呼び出してください。
        /// </summary>
        public void ApplyTileSideFaceMasks()
        {
            for( int y = 0; y < _tileRowNum; y++ )
            {
                for( int x = 0; x < _tileColNum; x++ )
                {
                    var tile = GetTile( x, y );
                    if( !tile.UsesSideFaceCulling ) { continue; }

                    TileType type = tile.StaticData().TileType;

                    // 隣が同種なら 0（面を消す）、そうでなければ 1（面を見せる）
                    Vector4 dirs = new Vector4(
                        IsSameTypeTileAt( x + 1, y, type ) ? 0f : 1f,   // +X(右)
                        IsSameTypeTileAt( x - 1, y, type ) ? 0f : 1f,   // -X(左)
                        IsSameTypeTileAt( x, y + 1, type ) ? 0f : 1f,   // +Z(前)
                        IsSameTypeTileAt( x, y - 1, type ) ? 0f : 1f    // -Z(後)
                    );

                    tile.ApplySideFaceMask( dirs );
                }
            }
        }

        /// <summary>
        /// 指定グリッド座標が範囲内かつ指定タイプのタイルであるかを返します（範囲外は false）
        /// </summary>
        private bool IsSameTypeTileAt( int x, int y, TileType type )
        {
            if( x < 0 || _tileColNum <= x || y < 0 || _tileRowNum <= y ) { return false; }

            return GetTile( x, y ).StaticData().TileType == type;
        }

        /// <summary>
        /// セーブ用データをセットアップします
        /// </summary>
        public void SetupSaveData()
        {
            for( int i = 0; i < GetTileTotalNum(); ++i )
            {
                _tileSaveDatas[i] = _tiles[i].ToSaveData();
            }
        }

        public float WidthX() { return TILE_SIZE * _tileColNum; }
        public float WidthZ() { return TILE_SIZE * _tileRowNum; }
        public int GetTileTotalNum() { return _tileRowNum * _tileColNum; }
        public Tile GetTile( int index ) => _tiles[index];
        public Tile GetTile( int x, int y ) => _tiles[x + ( y * _tileColNum )];
        public Tile[] Tiles => _tiles;

        public TileStaticData GetTileStaticData( int index )
        {
            return _tiles[index].StaticData();
        }

        public TileDynamicData GetTileDynamicData( int index )
        {
            return _tiles[index].DynamicData();
        }

        /// <summary>
        /// BehaviourとMeshを除いた状態で複製したStageDataを生成します
        /// </summary>
        /// <returns></returns>
        public TileDynamicData[] DeepCloneStageDynamicData()
        {
            TileDynamicData[] retDatas = new TileDynamicData[_tiles.Length];

            for( int x = 0; x < TileColNum; x++ )
            {
                for( int y = 0; y < TileRowNum; y++ )
                {
                    retDatas[x + ( y * TileColNum )] = this.GetTile( x, y ).DynamicData().DeepClone();
                }
            }

            return retDatas;
        }

        public void SetTile( int x, int y, Tile tile ) => _tiles[x + ( y * _tileColNum )] = tile;
        public void SetMaxDeployableUnits( int maxDeployableUnits ) => _maxDeployableUnits = maxDeployableUnits;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        public TileSaveData GetSaveData( int x, int y ) => _tileSaveDatas[x + ( y * _tileColNum )];
#endif // DEVELOPMENT_BUILD || UNITY_EDITOR
    }
}