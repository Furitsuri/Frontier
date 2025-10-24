using System;
using UnityEngine;
using static Constants;

namespace Frontier.Stage
{
    [Serializable]
    public class TileStaticData
    {
        [SerializeField] public float Height;           // 高さ
        [SerializeField] public int MoveResist;         // 移動阻害値
        [SerializeField] public Vector3 CharaStandPos;  // キャラクターの立ち位置座標
        [SerializeField] public TileType TileType;      // タイプ(芝生、砂漠、荒野など)

        /// <summary>
        /// 初期化します
        /// </summary>
        public void Init( int x, int y, float height, TileType tileType )
        {
            Height                  = height;
            MoveResist              = -1;
            float charaPosCorrext   = 0.5f * TILE_SIZE;                     // グリッド位置からキャラの立ち位置への補正値
            float posX              = x * TILE_SIZE + charaPosCorrext;
            float posZ              = y * TILE_SIZE + charaPosCorrext;
            CharaStandPos           = new Vector3( posX, Height, posZ );    // 上記の値から各グリッドのキャラの立ち位置を決定
            TileType                = tileType;
        }
    }
}