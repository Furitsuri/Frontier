using UnityEngine;

namespace Frontier.Entities
{
    /// <summary>
    /// 各ステータスを1上げるために必要なStatusPointのテーブルです。Resources/StatusGrowthData/StatusGrowthData.json から読み込みます。
    /// 本来はキャラクターのクラス毎に異なる想定ですが、クラスを厳密に定義していない現状は
    /// 全キャラクター共通の1テーブルのみを参照します(クラス対応時はキー等の拡張が必要です)。
    /// </summary>
    static public class StatusGrowthData
    {
        [System.Serializable]
        public struct Data
        {
            public int MaxHP;           // MaxHPを1上げるために必要なStatusPoint
            public int Atk;              // Atkを1上げるために必要なStatusPoint
            public int Def;              // Defを1上げるために必要なStatusPoint
            public int moveRange;        // moveRangeを1上げるために必要なStatusPoint
            public int jumpForce;        // jumpForceを1上げるために必要なStatusPoint
            public int maxActionGauge;   // maxActionGaugeを1上げるために必要なStatusPoint
        }

        private const string ResourcesPath = "StatusGrowthData/StatusGrowthData";

        static public Data data;

        static StatusGrowthData()
        {
            Load();
        }

        /// <summary>
        /// Resources/StatusGrowthData/StatusGrowthData.json からテーブルを読み込みます。
        /// </summary>
        static private void Load()
        {
            var asset = Resources.Load<TextAsset>( ResourcesPath );
            if ( asset == null )
            {
                Debug.LogWarning( $"[StatusGrowthData] ステータス成長テーブルが見つかりません: Resources/{ResourcesPath}.json" );
                return;
            }

            data = JsonUtility.FromJson<Data>( asset.text );
        }
    }
}
