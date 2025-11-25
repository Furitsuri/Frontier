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
        [SerializeField] private int _tileRowNum;                           // タイルの行数
        [SerializeField] private int _tileColNum;                           // タイルの列数
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        [SerializeField] private TileSaveData[] _tileSaveDatas = null;      // タイル内データのうち保存する必要のあるデータ
#endif // DEVELOPMENT_BUILD || UNITY_EDITOR

        [Inject] private HierarchyBuilderBase _hierarchyBld = null;

        private Tile[] _tiles = null;           // タイル

        public int TileRowNum => _tileRowNum;
        public int TileColNum => _tileColNum;

        public void Init( int tileRowNum, int tileColumnNum )
        {
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

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        public TileSaveData GetSaveData( int x, int y ) => _tileSaveDatas[x + ( y * _tileColNum )];
#endif // DEVELOPMENT_BUILD || UNITY_EDITOR
    }
}