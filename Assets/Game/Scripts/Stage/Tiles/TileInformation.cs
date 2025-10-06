using Frontier.Entities;
using UnityEngine;

namespace Frontier.Stage
{
    /// <summary>
    /// タイル単位における情報
    /// </summary>
    public class TileInformation
    {
        // キャラクターの立ち位置座標(※) ※は一度設定された後は変更することがない変数
        public Vector3 charaStandPos;
        // 移動阻害値(※)
        public int moveResist;
        // 移動値の見積もり値
        public int estimatedMoveRange;
        // グリッド上に存在するキャラクターのタイプ
        public CHARACTER_TAG charaTag;
        // グリッド上に存在するキャラクターのインデックス
        public int charaIndex;
        // フラグ情報
        public TileBitFlag flag;
        // この情報に紐づくキャラクターのキー情報
        public CharacterHashtable.Key CharaKey;

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
            charaTag            = CHARACTER_TAG.NONE;
            charaIndex          = -1;
            flag                = Stage.TileBitFlag.NONE;
        }

        /// <summary>
        /// グリッド上に存在しているキャラクターの情報を設定します
        /// </summary>
        /// <param name="chara">設定するキャラクター</param>
        public void SetExistCharacter( Character chara )
        {
            charaTag    = chara.Params.CharacterParam.characterTag;
            charaIndex  = chara.Params.CharacterParam.characterIndex;
        }

        /// <summary>
        /// グリッド上にキャラクターが存在するか否かを取得します
        /// </summary>
        /// <returns>グリッド上にキャラクターの存在しているか</returns>
        public bool IsExistCharacter()
        {
            return 0 <= charaIndex;
        }

        /// <summary>
        /// グリッド上に存在するキャラクターが指定のキャラクターと合致しているかを取得します
        /// </summary>
        /// <param name="chara">指定するキャラクター</param>
        /// <returns>合致しているか否か</returns>
        public bool IsMatchExistCharacter( Character chara )
        {
            return ( charaTag == chara.Params.CharacterParam.characterTag && charaIndex == chara.Params.CharacterParam.characterIndex );
        }

        /// <summary>
        /// 現在の値をコピーして対象に渡します
        /// </summary>
        /// <returns>値をコピーしたオブジェクト</returns>
        public TileInformation Copy()
        {
            TileInformation info           = new TileInformation();
            info.charaStandPos      = charaStandPos;
            info.moveResist         = moveResist;
            info.estimatedMoveRange = estimatedMoveRange;
            info.charaTag           = charaTag;
            info.charaIndex         = charaIndex;
            info.flag               = flag;

            return info;
        }
    }
}