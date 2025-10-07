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
        // タイル上に存在するキャラクターのハッシュキー
        public CharacterKey CharaKey;
        // フラグ情報
        public TileBitFlag flag;

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
            CharaKey            = new CharacterKey( CHARACTER_TAG.NONE, -1 );
            flag                = Stage.TileBitFlag.NONE;
        }

        /// <summary>
        /// グリッド上に存在しているキャラクターの情報を設定します
        /// </summary>
        /// <param name="chara">設定するキャラクター</param>
        public void SetExistCharacter( Character chara )
        {
            CharaKey = new CharacterKey( chara.Params.CharacterParam.characterTag, chara.Params.CharacterParam.characterIndex );
        }

        /// <summary>
        /// グリッド上にキャラクターが存在するか否かを取得します
        /// </summary>
        /// <returns>グリッド上にキャラクターの存在しているか</returns>
        public bool IsExistCharacter()
        {
            return CharaKey != new CharacterKey( CHARACTER_TAG.NONE, -1 );
        }

        /// <summary>
        /// グリッド上に存在するキャラクターが指定のキャラクターと合致しているかを取得します
        /// </summary>
        /// <param name="chara">指定するキャラクター</param>
        /// <returns>合致しているか否か</returns>
        public bool IsMatchExistCharacter( Character chara )
        {
            return ( CharaKey.CharacterTag == chara.Params.CharacterParam.characterTag && CharaKey.CharacterIndex == chara.Params.CharacterParam.characterIndex );
        }

        /// <summary>
        /// 現在の値をコピーして対象に渡します
        /// </summary>
        /// <returns>値をコピーしたオブジェクト</returns>
        public TileInformation Copy()
        {
            TileInformation info    = new TileInformation();
            info.charaStandPos      = charaStandPos;
            info.moveResist         = moveResist;
            info.estimatedMoveRange = estimatedMoveRange;
            info.CharaKey           = CharaKey;
            info.flag               = flag;

            return info;
        }
    }
}