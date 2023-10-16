using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier.Stage
{
    /// <summary>
    /// ステージ上のグリッド数などのデータ
    /// </summary>
    public class StageModel : MonoBehaviour
    {
        [SerializeField]
        private int _gridRowNum;

        [SerializeField]
        private int _gridColumnNum;

        [SerializeField]
        private float _gridSize = 1f;

        public float WidthX { get; set; }
        public float WidthZ { get; set; }

        public int GetGridRowNum() { return _gridRowNum; }

        public int GetGridColumnNum() {  return _gridColumnNum; }

        public float GetGridSize() { return _gridSize; }

        public void SetGridRowNum( int rowNum ) {  _gridRowNum = rowNum;}

        public void SetGridColumnNum( int columnNum ) { _gridColumnNum = columnNum; }
    }
}