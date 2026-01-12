using Frontier.Entities;
using System;
using UnityEngine;

namespace Frontier.Combat.Skill
{
    /// <summary>
    /// 各スキルの実行内容の関数集合です
    /// </summary>
    static public class SkillsData
    {
        [System.Serializable]
        public struct Data
        {
            public string Name;
            public int Cost;            // 使用コスト
            public SituationType Type;  // 使用シチュエーションタイプ
            public int Flags;           // スキルフラグ
            public int Duration;
            public float AddAtkMag;     // 攻撃力倍率加算
            public float AddDefMag;     // 防御力倍率加算
            public int AddAtkNum;       // 攻撃回数加算
            public float Param1;
            public float Param2;
            public float Param3;
            public float Param4;
            public string ExplainTextKey;
        }

        static public Data[] data                                       = new Data[(int)ID.SKILL_NUM];
        static public Func<SkillNotifierBase>[] skillNotifierFactory    = null;
        private static readonly SkillNotifierBase sharedNotifier        = new SkillNotifierBase();  // 使いまわし前提のため静的読み取り専用

        static public void BuildSkillNotifierFactory( HierarchyBuilderBase hierarchyBld )
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
        static public void ExecGuard(ref ModifiedParameter modifiedParam, ref CharacterParameter param)
        {
            modifiedParam.Def = (int)Mathf.Floor(param.Def * 0.5f);
            param.consumptionActionGauge += 1;
        }
    }
}