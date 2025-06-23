using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier.Stage
{
    /// <summary>
    /// ステージ上のグリッド数などのデータ
    /// </summary>
    public class StageData : MonoBehaviour
    {
        [SerializeField]
        private int _gridRowNum;

        [SerializeField]
        private int _gridColumnNum;

        [SerializeField]
        private float _gridSize = 1f;

        public List<StageTileData> tiles = new();

        public float WidthX { get; set; }
        public float WidthZ { get; set; }

        public int GetGridRowNum() { return _gridRowNum; }

        public int GetGridColumnNum() {  return _gridColumnNum; }

        public float GetGridSize() { return _gridSize; }

        public void SetGridRowNum( int rowNum ) {  _gridRowNum = rowNum;}

        public void SetGridColumnNum( int columnNum ) { _gridColumnNum = columnNum; }

#if UNITY_EDITOR
        public int Width;
        public int Height;
        public StageTile[] Tiles;

        public StageTile GetTile(int x, int y) => Tiles[y * Width + x];

        public void SetTile(int x, int y, StageTile tile) => Tiles[y * Width + x] = tile;
#endif // UNITY_EDITOR
    }
}