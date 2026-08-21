using UnityEngine;

namespace Frontier.Entities
{
    /// <summary>
    /// 敵種別ごとの撃破報酬(アニマ)テーブルです。Resources/EnemyRewardData/EnemyRewardData.json から読み込みます。
    /// 配列のインデックスは PrefabRegistry.EnemyPrefabs の並びと対応する敵種別を表します
    /// (Status.PrefabIndexは CHARACTER_TAG.ENEMY の場合にこの敵種別を指します)。
    /// 報酬値はレベルに対して線形に算出します : BaseAnima + AnimaPerLevel × (Level - 1)
    /// </summary>
    static public class EnemyRewardData
    {
        [System.Serializable]
        public struct Data
        {
            public int BaseAnima;        // Lv1時点の基礎アニマ
            public float AnimaPerLevel;  // レベル1上昇あたりのアニマ増加量
        }

        // JsonUtility.FromJson()によるJSONデシリアライズでのみ値が設定されるフィールドのため、
        // C#コード上では代入箇所が無くCS0649(未割り当て)警告が出る。意図的なものとして抑制する。
#pragma warning disable 0649
        [System.Serializable]
        private class EnemyRewardDataContainer
        {
            public Data[] Enemies;
        }
#pragma warning restore 0649

        private const string ResourcesPath = "EnemyRewardData/EnemyRewardData";

        static public Data[] data = null;

        static EnemyRewardData()
        {
            Load();
        }

        /// <summary>
        /// Resources/EnemyRewardData/EnemyRewardData.json からテーブルを読み込みます。
        /// </summary>
        static private void Load()
        {
            var asset = Resources.Load<TextAsset>( ResourcesPath );
            if ( asset == null )
            {
                Debug.LogWarning( $"[EnemyRewardData] 敵報酬データが見つかりません: Resources/{ResourcesPath}.json" );
                return;
            }

            var container = JsonUtility.FromJson<EnemyRewardDataContainer>( asset.text );
            if ( container == null || container.Enemies == null )
            {
                Debug.LogWarning( "[EnemyRewardData] 敵報酬データの読み込みに失敗しました" );
                return;
            }

            data = container.Enemies;
        }

        /// <summary>
        /// 指定した敵種別・レベルを撃破した際に得られるアニマを算出します。
        /// </summary>
        /// <param name="enemyPrefabIndex">敵のプレハブインデックス(Status.PrefabIndex)</param>
        /// <param name="level">敵のレベル(Status.Level)</param>
        static public int CalcAnima( int enemyPrefabIndex, int level )
        {
            if ( !IsValidIndex( enemyPrefabIndex ) ) { return 0; }

            var d = data[enemyPrefabIndex];
            return Mathf.FloorToInt( d.BaseAnima + d.AnimaPerLevel * ( level - 1 ) );
        }

        static private bool IsValidIndex( int enemyPrefabIndex )
        {
            if ( data == null || enemyPrefabIndex < 0 || data.Length <= enemyPrefabIndex )
            {
                Debug.LogWarning( $"[EnemyRewardData] 不正な敵プレハブインデックスです: {enemyPrefabIndex}" );
                return false;
            }

            return true;
        }
    }
}
