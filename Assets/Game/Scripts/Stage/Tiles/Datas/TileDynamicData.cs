using Frontier.Entities;
using Zenject;

namespace Frontier.Stage
{
    public class TileDynamicData
    {
        [Inject] HierarchyBuilderBase _hierarchyBld = null;

        // 移動値の見積もり値
        public int EstimatedMoveRange;
        // タイル上に存在するキャラクターのハッシュキー
        public CharacterKey CharaKey;
        // フラグ情報
        public TileBitFlag Flag;

        /// <summary>
        /// 初期化します
        /// TODO：ステージのファイル読込によってmoveRangeをはじめとした値を設定出来るようにしたい
        ///       また、C# 10.0 からは引数なしコンストラクタで定義可能(2023.5時点の最新Unityバージョンでは使用できない)
        /// </summary>
        public void Init()
        {
            EstimatedMoveRange  = -1;
            CharaKey            = new CharacterKey( CHARACTER_TAG.NONE, -1 );
            Flag                = Stage.TileBitFlag.NONE;
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
        /// 現在の状態をクローンして対象に渡します
        /// </summary>
        /// <returns>クローンオブジェクト</returns>
        public TileDynamicData DeepClone()
        {
            TileDynamicData retData     = _hierarchyBld.InstantiateWithDiContainer<TileDynamicData>( false );
            retData.EstimatedMoveRange  = this.EstimatedMoveRange;
            retData.CharaKey            = this.CharaKey;
            retData.Flag                = this.Flag;

            return retData;
        }
    }
}