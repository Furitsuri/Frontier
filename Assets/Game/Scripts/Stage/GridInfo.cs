using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Frontier.Character;
using static Frontier.Stage.StageController;

namespace Frontier.Stage
{
    /// <summary>
    /// グリッド単位における情報
    /// </summary>
    public class GridInfo
    {
        // キャラクターの立ち位置座標(※)
        public Vector3 charaStandPos;
        // 移動阻害値(※)
        public int moveResist;
        // 移動値の見積もり値
        public int estimatedMoveRange;
        // グリッド上に存在するキャラクターのタイプ
        public CHARACTER_TAG characterTag;
        // グリッド上に存在するキャラクターのインデックス
        public int charaIndex;
        // フラグ情報
        public BitFlag flag;
        // ※ 一度設定された後は変更することがない変数

        /// <summary>
        /// 初期化します
        /// TODO：ステージのファイル読込によってmoveRangeをはじめとした値を設定出来るようにしたい
        ///       また、C# 10.0 からは引数なしコンストラクタで定義可能(2023.5時点の最新Unityバージョンでは使用できない)
        /// </summary>
        public void Init()
        {
            charaStandPos       = Vector3.zero;
            moveResist          = -1;
            estimatedMoveRange  = -1;
            characterTag        = CHARACTER_TAG.NONE;
            charaIndex          = -1;
            flag                = BitFlag.NONE;
        }

        /// <summary>
        /// グリッド上にキャラクターが存在するか否かを返します
        /// </summary>
        /// <returns>グリッド上にキャラクターの存在しているか</returns>
        public bool IsExistCharacter()
        {
            return 0 <= charaIndex;
        }

        /// <summary>
        /// 現在の値をコピーして対象に渡します
        /// </summary>
        /// <returns>値をコピーしたオブジェクト</returns>
        public GridInfo Copy()
        {
            GridInfo info           = new GridInfo();
            info.charaStandPos      = charaStandPos;
            info.moveResist         = moveResist;
            info.estimatedMoveRange = estimatedMoveRange;
            info.characterTag       = characterTag;
            info.charaIndex         = charaIndex;
            info.flag               = flag;

            return info;
        }
    }
}