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
        public Vector3 CharaStandPos;  // キャラクターの立ち位置座標（タイプ別の高さ補正を含む。水なら沈む）
        public Vector3 CursorStandPos; // グリッドカーソルの位置座標（高さ補正を含まない素のタイル高さ）
        public TileType TileType;      // タイプ(芝生、砂漠、荒野など)

        /// <summary>
        /// 初期化します
        /// </summary>
        public void Init( int x, int y, bool isDeployable, float height, TileType tileType )
        {
            IsDeployable            = isDeployable;
            Height                  = height;
            MoveResist              = 1;
            float charaPosCorrext   = 0.5f * TILE_SIZE;                     // グリッド位置からキャラの立ち位置への補正値
            float posX              = x * TILE_SIZE + charaPosCorrext;
            float posZ              = y * TILE_SIZE + charaPosCorrext;
            // タイプごとの立ち位置Y補正（水なら負の値で沈み、水に浸かった表現になる）
            float charaStandY       = Height + TileMaterialLibrary.GetProfile( tileType ).CharaStandHeightOffset;
            CharaStandPos           = new Vector3( posX, charaStandY, posZ ); // キャラ用は補正込み
            CursorStandPos          = new Vector3( posX, Height, posZ );      // グリッドカーソル用は補正なし（素のタイル高さ）
            TileType                = tileType;
        }
    }
}