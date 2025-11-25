using System;
using UnityEngine;
using static Constants;

namespace Frontier.Stage
{
    public class TileStaticData
    {
        public bool IsDeployable;      // 配備可能かどうか 
        public float Height;           // 高さ
        public int MoveResist;         // 移動阻害値
        public Vector3 CharaStandPos;  // キャラクターの立ち位置座標
        public TileType TileType;      // タイプ(芝生、砂漠、荒野など)

        /// <summary>
        /// 初期化します
        /// </summary>
        public void Init( int x, int y, bool isDeployable, float height, TileType tileType )
        {
            IsDeployable            = isDeployable;
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