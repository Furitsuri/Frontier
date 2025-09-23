using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using static Constants;
using static Frontier.DebugTools.StageEditor.StageEditorController;

namespace Frontier.Stage
{
    /// <summary>
    /// ステージ上のグリッド数などのデータ
    /// </summary>
    [System.Serializable]
    public class StageData
    {
        [SerializeField]
        private int _gridRowNum;                    // グリッドの行数
        [SerializeField]
        private int _gridColumnNum;                 // グリッドの列数
        [SerializeField]
        private StageTileData[] _tileDatas = null;  // タイルデータ

        [Inject] private HierarchyBuilderBase _hierarchyBld = null;

        public int GridRowNum => _gridRowNum;
        public int GridColumnNum => _gridColumnNum;

        public void Init( int tileRowNum, int tileColumnNum )
        {
            _gridRowNum     = tileRowNum;
            _gridColumnNum  = tileColumnNum;
            _tileDatas      = new StageTileData[_gridRowNum * _gridColumnNum];
        }

        public void Init( int tileRowNum, int tileColumnNum, float height, TileType type, GameObject[] tilePrefabs )
        {
            Init( tileRowNum, tileColumnNum );

            for ( int x = 0; x < tileColumnNum; x++ )
            {
                for ( int y = 0; y < tileRowNum; y++ )
                {
                    SetTile( x, y, _hierarchyBld.InstantiateWithDiContainer<StageTileData>( false ) );
                    GetTile( x, y ).Init( x, y, 0f, TileType.None, tilePrefabs );
                }
            }
        }

        public void Dispose()
        {
            if (_tileDatas != null)
            {
                foreach (var tile in _tileDatas)
                {
                    tile.Dispose();
                }

                _tileDatas = null;
            }
        }

        public StageTileData[] TileDatas => _tileDatas;
        public float WidthX() { return TILE_SIZE * _gridColumnNum; }
        public float WidthZ() { return TILE_SIZE * _gridRowNum; }

        public int GetTileTotalNum() { return _gridRowNum * _gridColumnNum; }

        public StageTileData GetTile(int x, int y) => _tileDatas[x + (y * _gridColumnNum)];

        public StageTileData GetTileData(int index) => _tileDatas[index];

        /// <summary>
        /// 指定された行のタイルデータをList状にしてまとめて取得します
        /// </summary>
        /// <param name="rowIndex">指定行</param>
        /// <returns>タイルデータ</returns>
        public List<StageTileData> GetTilesAtRow( int rowIndex )
        {
            List<StageTileData> stageTileDatas = new List<StageTileData>();

            if ( !rowIndex.IsBetween( 0, _gridRowNum - 1 ) )
            {
                Debug.LogError("指定されたインデックス値がデータの範囲外のものになっています。");
                return stageTileDatas;
            }

            for( int i = 0; i < _tileDatas.Length; ++i )
            {
                if( i.IsBetween( rowIndex * _gridColumnNum, ( rowIndex + 1 ) * _gridColumnNum - 1 ) )
                {
                    stageTileDatas.Add( _tileDatas[i] );
                }
            }

            return stageTileDatas;
        }

        /// <summary>
        /// 指定された列のタイルデータをList状にしてまとめて取得します
        /// </summary>
        /// <param name="colIndex">指定列</param>
        /// <returns>タイルデータ</returns>
        public List<StageTileData> GetTilesAtCol( int colIndex )
        {
            List<StageTileData> stageTileDatas = new List<StageTileData>();

            if ( !colIndex.IsBetween( 0, _gridColumnNum - 1 ) )
            {
                Debug.LogError( "指定されたインデックス値がデータの範囲外のものになっています。" );
                return stageTileDatas;
            }

            for ( int i = 0; i < _tileDatas.Length; ++i )
            {
                if ( i % colIndex == 0 )
                {
                    stageTileDatas.Add( _tileDatas[i] );
                }
            }

            return stageTileDatas;
        }

        public ref TileInformation GetTileInfo(int index)
        {
            return ref _tileDatas[index].GetTileInfo();
        }

        public void SetGridRowNum( int rowNum ) {  _gridRowNum = rowNum;}

        public void SetGridColNum( int colNum ) { _gridColumnNum = colNum; }

        public void SetTile(int index, StageTileData tile) => _tileDatas[index] = tile;

        public void SetTile(int x, int y, StageTileData tile) => _tileDatas[x + (y * _gridColumnNum )] = tile;
    }
}