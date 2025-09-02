using Frontier.Entities;
using System;
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

        public static Data[] data                                       = new Data[(int)ID.SKILL_NUM];
        public static Func<SkillNotifierBase>[] skillNotifierFactory    = null;
        private static readonly SkillNotifierBase sharedNotifier        = new SkillNotifierBase();  // 使いまわし前提のため静的読み取り専用

        public static void BuildSkillNotifierFactory( HierarchyBuilderBase hierarchyBld )
        {
            if ( skillNotifierFactory != null ) { return; }

            // MEMO : バフなどのDataのみで対応可能なものは何もする必要がないため、
            //        ベースクラスであるSkillNotifierBaseで対応しています。
            Func<SkillNotifierBase>[] factories = new Func<SkillNotifierBase>[(int)ID.SKILL_NUM]
            {
                () => hierarchyBld.InstantiateWithDiContainer<ParrySkillNotifier>(false),   // SKILL_PARRY
                () => sharedNotifier,                                                       // SKILL_GUARD
                () => sharedNotifier,                                                       // SKILL_COUNTER
                () => sharedNotifier,                                                       // SKILL_DOUBLE_STRIKE
                () => sharedNotifier,                                                       // SKILL_TRIPLE_STRIKE
            };

            skillNotifierFactory = factories;
        }

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