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
    public class StageData
    {
        public int GridRowNum { get; private set; }                     // グリッドの行数
        public int GridColumnNum { get; private set; }                  // グリッドの列数
        public StageTileData[] TileDatas { get; private set; } = null;  // タイルデータ

        public void Init( int tileRowNum, int tileColumnNum )
        {
            GridRowNum     = tileRowNum;
            GridColumnNum  = tileColumnNum;
            TileDatas      = new StageTileData[GridRowNum * GridColumnNum];
        }

        public float WidthX() { return TILE_SIZE * GridRowNum; }
        public float WidthZ() { return TILE_SIZE * GridColumnNum; }

        public int GetGridToralNum() { return GridRowNum * GridColumnNum; }

        public StageTileData GetTile(int x, int y) => TileDatas[x + y * GridRowNum];

        public StageTileData GetTile(int index) => TileDatas[index];

        public ref GridInfo GetTileInfo(int index)
        {
            return ref TileDatas[index].GetTileInfo();
        }

        public void SetGridRowNum( int rowNum ) {  GridRowNum = rowNum;}

        public void SetGridColumnNum( int columnNum ) { GridColumnNum = columnNum; }

        public void SetTile(int index, StageTileData tile) => TileDatas[index] = tile;

        public void SetTile(int x, int y, StageTileData tile) => TileDatas[y * GridRowNum + x] = tile;
    }
}