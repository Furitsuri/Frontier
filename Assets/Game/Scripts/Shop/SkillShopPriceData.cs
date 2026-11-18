using Frontier.Combat;
using UnityEngine;

namespace Frontier.Shop
{
    /// <summary>
    /// スキルをショップで販売する際の価格マスターデータです。
    /// ActiveSkillsData.Costは戦闘中の使用コストであり購入価格ではないため、別テーブルとして独立させています。
    /// ActiveSkillsDataと同じ「static自己ロード」パターンに倣い、アクティブスキルは
    /// Resources/ShopData/ActiveSkillShopPriceData.json、パッシブスキルはResources/ShopData/PassiveSkillShopPriceData.jsonから読み込みます。
    /// JSON配列の並び順は、それぞれActiveSkillID/PassiveSkillID enumの宣言順と対応させてください。
    /// </summary>
    static public class SkillShopPriceData
    {
        [System.Serializable]
        private struct FileData
        {
            public int Price;
        }

        [System.Serializable]
        private class PriceDataContainer
        {
            public FileData[] Prices;
        }

        private const string ActiveResourcesPath  = "ShopData/ActiveSkillShopPriceData";
        private const string PassiveResourcesPath = "ShopData/PassiveSkillShopPriceData";

        static private readonly int[] _activePrices  = new int[( int ) ActiveSkillID.NUM];
        static private readonly int[] _passivePrices = new int[( int ) PassiveSkillID.NUM];

        static SkillShopPriceData()
        {
            Load( ActiveResourcesPath, _activePrices );
            Load( PassiveResourcesPath, _passivePrices );
        }

        static public int GetPrice( ActiveSkillID skillID )  => ActiveSkillsData.IsValidSkill( skillID ) ? _activePrices[( int ) skillID] : 0;

        static public int GetPrice( PassiveSkillID skillID ) => PassiveSkillsData.IsValidSkill( skillID ) ? _passivePrices[( int ) skillID] : 0;

        static private void Load( string resourcesPath, int[] dest )
        {
            var asset = Resources.Load<TextAsset>( resourcesPath );
            if ( asset == null )
            {
                Debug.LogWarning( $"[SkillShopPriceData] 価格データが見つかりません: Resources/{resourcesPath}.json" );
                return;
            }

            var container = JsonUtility.FromJson<PriceDataContainer>( asset.text );
            if ( container == null || container.Prices == null )
            {
                Debug.LogWarning( $"[SkillShopPriceData] 価格データの読み込みに失敗しました: Resources/{resourcesPath}.json" );
                return;
            }

            int count = Mathf.Min( container.Prices.Length, dest.Length );
            for ( int i = 0; i < count; ++i )
            {
                dest[i] = container.Prices[i].Price;
            }
        }
    }
}
