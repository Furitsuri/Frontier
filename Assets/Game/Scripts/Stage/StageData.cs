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

        /*
        public void CopyFrom(StageData other)
        {
            _gridRowNum     = other._gridRowNum;
            _gridColumnNum  = other._gridColumnNum;
            if (_tileDatas == null || _tileDatas.Length != other._tileDatas.Length)
            {
                _tileDatas = new StageTileData[other._tileDatas.Length];
            }
            for (int i = 0; i < other._tileDatas.Length; i++)
            {
                if (_tileDatas[i] == null)
                {
                    _tileDatas[i] = new StageTileData();
                }
                _tileDatas[i].CopyFrom(other._tileDatas[i]);
            }
        }*/

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