using UnityEngine;

namespace Frontier.Entities
{
    /// <summary>
    /// レベル毎の必要累計経験値のテーブルです。Resources/LevelExpData/LevelExpData.json から読み込みます。
    /// 現状のJSONの中身は仮のデータ(RequiredTotalExp = 50 × Level × (Level - 1)。
    /// レベルが上がるほど1レベルあたりの必要量も増えていく単純な二次式)です。今後バランス調整時に差し替えてください。
    /// </summary>
    static public class LevelExpData
    {
        [System.Serializable]
        public struct Data
        {
            public int RequiredTotalExp;  // このレベルに到達するために必要な累計経験値
        }

        [System.Serializable]
        private class LevelExpDataContainer
        {
            public Data[] Levels;
        }

        private const string ResourcesPath = "LevelExpData/LevelExpData";

        // インデックス0は未使用(レベルは1始まりのため)。1 〜 Constants.MAX_LEVEL まで使用します
        static public Data[] data = new Data[Constants.MAX_LEVEL + 1];

        static LevelExpData()
        {
            Load();
        }

        /// <summary>
        /// Resources/LevelExpData/LevelExpData.json からテーブルを読み込みます。
        /// </summary>
        static private void Load()
        {
            var asset = Resources.Load<TextAsset>( ResourcesPath );
            if ( asset == null )
            {
                Debug.LogWarning( $"[LevelExpData] レベル経験値テーブルが見つかりません: Resources/{ResourcesPath}.json" );
                return;
            }

            var container = JsonUtility.FromJson<LevelExpDataContainer>( asset.text );
            if ( container == null || container.Levels == null )
            {
                Debug.LogWarning( "[LevelExpData] レベル経験値テーブルの読み込みに失敗しました" );
                return;
            }

            int count = Mathf.Min( container.Levels.Length, data.Length );
            for ( int i = 0; i < count; ++i )
            {
                data[i] = container.Levels[i];
            }
        }

        /// <summary>
        /// 既に最大レベルに到達しているかどうかを返します
        /// </summary>
        static public bool IsMaxLevel( int level )
        {
            return Constants.MAX_LEVEL <= level;
        }

        /// <summary>
        /// 現在のレベル・累計経験値を基に、次のレベルまでに必要な残り経験値を返します。
        /// 既に最大レベルに到達している場合は0を返します。
        /// </summary>
        /// <param name="level">現在のレベル</param>
        /// <param name="currentExp">現在の累計取得経験値</param>
        static public int GetExpToNextLevel( int level, int currentExp )
        {
            if ( IsMaxLevel( level ) ) { return 0; }

            int requiredExpForNextLevel = data[level + 1].RequiredTotalExp;

            return Mathf.Max( 0, requiredExpForNextLevel - currentExp );
        }
    }
}
