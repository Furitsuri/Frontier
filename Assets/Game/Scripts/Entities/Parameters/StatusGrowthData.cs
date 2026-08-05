using UnityEngine;

namespace Frontier.Entities
{
    /// <summary>
    /// 各ステータスの成長設定(1上げるために必要なStatusPoint、取り得る最小値・最大値)のテーブルです。
    /// Resources/StatusGrowthData/StatusGrowthData.json から読み込みます。
    /// 本来はキャラクターのクラス毎に異なる想定ですが、クラスを厳密に定義していない現状は
    /// 全キャラクター共通の1テーブルのみを参照します(クラス対応時はキー等の拡張が必要です)。
    /// </summary>
    static public class StatusGrowthData
    {
        [System.Serializable]
        public struct StatRange
        {
            public int Cost;  // このステータスを1上げるために必要なStatusPoint
            public int Min;   // このステータスが取り得る最小値
            public int Max;   // このステータスが取り得る最大値
        }

        [System.Serializable]
        public struct Data
        {
            public StatRange MaxHP;
            public StatRange Atk;
            public StatRange Def;
            public StatRange moveRange;
            public StatRange jumpForce;
            public StatRange maxActionGauge;
            public StatRange recoveryActionGauge;
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
