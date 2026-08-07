using UnityEngine;

namespace Frontier.Entities
{
    /// <summary>
    /// レベルアップ時に得られるStatusPointのテーブルです。Resources/LevelUpStatusPointData/LevelUpStatusPointData.json から読み込みます。
    /// 現状のJSONの中身は初期値5、レベルの十の位が1上がる毎に1ずつ増加する仮のデータ
    /// (Lv.2〜9は5、Lv.10〜19は6、Lv.20〜29は7、…)です。今後バランス調整時に差し替えてください。
    /// </summary>
    static public class LevelUpStatusPointData
    {
        [System.Serializable]
        public struct Data
        {
            public int GrantedStatusPoint;  // そのレベルに到達した際に得られるStatusPoint
        }

        // JsonUtility.FromJson()によるJSONデシリアライズでのみ値が設定されるフィールドのため、
        // C#コード上では代入箇所が無くCS0649(未割り当て)警告が出る。意図的なものとして抑制する。
#pragma warning disable 0649
        [System.Serializable]
        private class LevelUpStatusPointDataContainer
        {
            public Data[] Levels;
        }
#pragma warning restore 0649

        private const string ResourcesPath = "LevelUpStatusPointData/LevelUpStatusPointData";

        // インデックス0は未使用(レベルは1始まりのため)。1 〜 Constants.MAX_LEVEL まで使用します
        static public Data[] data = new Data[Constants.MAX_LEVEL + 1];

        static LevelUpStatusPointData()
        {
            Load();
        }

        /// <summary>
        /// Resources/LevelUpStatusPointData/LevelUpStatusPointData.json からテーブルを読み込みます。
        /// </summary>
        static private void Load()
        {
            var asset = Resources.Load<TextAsset>( ResourcesPath );
            if ( asset == null )
            {
                Debug.LogWarning( $"[LevelUpStatusPointData] レベルアップ時のStatusPointテーブルが見つかりません: Resources/{ResourcesPath}.json" );
                return;
            }

            var container = JsonUtility.FromJson<LevelUpStatusPointDataContainer>( asset.text );
            if ( container == null || container.Levels == null )
            {
                Debug.LogWarning( "[LevelUpStatusPointData] レベルアップ時のStatusPointテーブルの読み込みに失敗しました" );
                return;
            }

            int count = Mathf.Min( container.Levels.Length, data.Length );
            for ( int i = 0; i < count; ++i )
            {
                data[i] = container.Levels[i];
            }
        }

        /// <summary>
        /// 指定レベルに到達した際に得られるStatusPointを返します。
        /// </summary>
        /// <param name="level">到達したレベル</param>
        static public int GetGrantedStatusPoint( int level )
        {
            if ( level < 0 || data.Length <= level ) { return 0; }

            return data[level].GrantedStatusPoint;
        }
    }
}
