using Frontier.Combat;
using UnityEngine;

namespace Frontier.Shop
{
    /// <summary>
    /// スキルをショップで販売する際の価格マスターデータです。
    /// SkillsData.Costは戦闘中の使用コストであり購入価格ではないため、別テーブルとして独立させています。
    /// SkillsDataと同じ「static自己ロード」パターンに倣い、Resources/ShopData/SkillShopPriceData.jsonから読み込みます。
    /// JSON配列の並び順はSkillID enumの宣言順と対応させてください(SkillsData.jsonと同じ規約)。
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

        private const string ResourcesPath = "ShopData/SkillShopPriceData";

        static public int[] Price = new int[( int ) SkillID.NUM];

        static SkillShopPriceData()
        {
            Load();
        }

        static private void Load()
        {
            var asset = Resources.Load<TextAsset>( ResourcesPath );
            if ( asset == null )
            {
                Debug.LogWarning( $"[SkillShopPriceData] 価格データが見つかりません: Resources/{ResourcesPath}.json" );
                return;
            }

            var container = JsonUtility.FromJson<PriceDataContainer>( asset.text );
            if ( container == null || container.Prices == null )
            {
                Debug.LogWarning( "[SkillShopPriceData] 価格データの読み込みに失敗しました" );
                return;
            }

            int count = Mathf.Min( container.Prices.Length, Price.Length );
            for ( int i = 0; i < count; ++i )
            {
                Price[i] = container.Prices[i].Price;
            }
        }
    }
}
