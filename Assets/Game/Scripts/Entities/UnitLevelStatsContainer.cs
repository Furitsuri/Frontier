using UnityEngine;
using static Frontier.BattleFileLoader;

namespace Frontier
{
    [System.Serializable]
    public class UnitStatusData
    {
        public CharacterStatusData[] StatusDatas;
    }

    [System.Serializable]
    public class UnitLevelStatsContainer
    {
        // [SerializeField] private TestData[][] Stats;
        // ※jsonは二次元配列非対応のため、この形にしています
        public UnitStatusData[] Stats;
    }
}