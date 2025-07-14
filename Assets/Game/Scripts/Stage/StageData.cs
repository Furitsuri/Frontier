using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.Stage
{
    /// <summary>
    /// ステージ上のグリッド数などのデータ
    /// </summary>
    [System.Serializable]
    public class StageData
    {
        [SerializeField]
        private int _gridRowNum;                     // グリッドの行数
        [SerializeField]
        private int _gridColumnNum;                  // グリッドの列数
        [SerializeField]
        private StageTileData[] _tileDatas = null;  // タイルデータ

        public int GridRowNum => _gridRowNum;
        public int GridColumnNum => _gridColumnNum;

        public void Init( int tileRowNum, int tileColumnNum )
        {
            _gridRowNum     = tileRowNum;
            _gridColumnNum  = tileColumnNum;
            _tileDatas      = new StageTileData[_gridRowNum * _gridColumnNum];
        }

        public StageTileData[] TileDatas => _tileDatas;
        public float WidthX() { return TILE_SIZE * _gridRowNum; }
        public float WidthZ() { return TILE_SIZE * _gridColumnNum; }

        public int GetGridToralNum() { return _gridRowNum * _gridColumnNum; }

        public StageTileData GetTile(int x, int y) => _tileDatas[x + y * _gridRowNum];

        public StageTileData GetTile(int index) => _tileDatas[index];

        public ref GridInfo GetTileInfo(int index)
        {
            return ref _tileDatas[index].GetTileInfo();
        }

        public void SetGridRowNum( int rowNum ) {  _gridRowNum = rowNum;}

        public void SetGridColumnNum( int columnNum ) { _gridColumnNum = columnNum; }

        public void SetTile(int index, StageTileData tile) => _tileDatas[index] = tile;

        public void SetTile(int x, int y, StageTileData tile) => _tileDatas[y * _gridRowNum + x] = tile;
    }
}