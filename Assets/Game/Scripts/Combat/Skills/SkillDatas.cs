using Frontier.Entities;
using UnityEngine;

namespace Frontier.Combat.Skill
{

    /// <summary>
    /// 各スキルの実行内容の関数集合です
    /// </summary>
    public static class SkillsData
    {
        [System.Serializable]
        public struct Data
        {
            public string Name;
            public int Cost;
            public SituationType Type;
            public int Duration;
            public float AddAtkMag;
            public float AddDefMag;
            public int AddAtkNum;
            public float Param1;
            public float Param2;
            public float Param3;
            public float Param4;
        }

        public static Data[] data = new Data[(int)ID.SKILL_NUM];

        /// <summary>
        /// ガードスキルの実行内容です
        /// </summary>
        /// <param name="modifiedParam">スキル使用キャラのバフ・デバフ用パラメータ</param>
        /// <param name="param">スキル使用キャラのパラメータ</param>
        public static void ExecGuard(ref ModifiedParameter modifiedParam, ref CharacterParameter param)
        {
            modifiedParam.Def = (int)Mathf.Floor(param.Def * 0.5f);
            param.consumptionActionGauge += 1;
        }
    }
}