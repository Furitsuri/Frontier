using Frontier.Entities;
using System;
using UnityEngine;

namespace Frontier.Combat
{
    /// <summary>
    /// 各スキルの実行内容の関数集合です
    /// </summary>
    static public class SkillsData
    {
        [System.Serializable]
        public struct Data
        {
            public string Name;                     // スキル名
            public int Cost;                        // 使用コスト
            public SituationType SituationType;     // 使用シチュエーションタイプ
            public ActionType ActionType;           // スキルタイプ
            public SkillBitFlag Flags;              // スキルフラグ
            public int Duration;                    // 効果時間（ターン数）
            public RangeType RangeType;             // レンジタイプ
            public int Range;                       // レンジ範囲
            public float AddAtkMag;                 // 攻撃力倍率加算
            public float AddDefMag;                 // 防御力倍率加算
            public int AddAtkNum;                   // 攻撃回数加算
            public float Param1;                    // 汎用パラメータ1
            public float Param2;                    // 汎用パラメータ2
            public float Param3;                    // 汎用パラメータ3
            public float Param4;                    // 汎用パラメータ4
            public string ExplainTextKey;           // スキル説明文のテキストキー
        }

        static public Data[] data = new Data[( int ) SkillID.NUM];
        static public Func<SkillNotifierBase>[] skillNotifierFactory = null;
        private static readonly SkillNotifierBase sharedNotifier = new SkillNotifierBase();  // 使いまわし前提のため静的読み取り専用

        static public void BuildSkillNotifierFactory( HierarchyBuilderBase hierarchyBld )
        {
            if( skillNotifierFactory != null ) { return; }

            // MEMO : バフなどのDataのみで対応可能なものは何もする必要がないため、
            //        ベースクラスであるSkillNotifierBaseで対応しています。
            Func<SkillNotifierBase>[] factories = new Func<SkillNotifierBase>[( int ) SkillID.NUM]
            {
                () => hierarchyBld.InstantiateWithDiContainer<ParrySkillNotifier>(false),   // SKILL_PARRY
                () => sharedNotifier,                                                       // SKILL_GUARD
                () => sharedNotifier,                                                       // SKILL_BUILD_UP
                () => sharedNotifier,                                                       // SKILL_COUNTER
                () => sharedNotifier,                                                       // SKILL_DOUBLE_STRIKE
                () => sharedNotifier,                                                       // SKILL_TRIPLE_STRIKE
                () => sharedNotifier,                                                       // SKILL_DASH_SLASH
            };

            skillNotifierFactory = factories;
        }

        /// <summary>
        /// ガードスキルの実行内容です
        /// </summary>
        /// <param name="modifiedParam">スキル使用キャラのバフ・デバフ用パラメータ</param>
        /// <param name="param">スキル使用キャラのパラメータ</param>
        static public void ExecGuard( ref ModifiedParameter modifiedParam, ref Status param )
        {
            modifiedParam.Def = ( int ) Mathf.Floor( param.Def * 0.5f );
            param.ActGaugeConsumption += 1;
        }

        static public bool IsValidSkill( SkillID skillID )
        {
            return SkillID.NONE < skillID && skillID < SkillID.NUM;
        }

        static public bool IsTransitionSkillActionType( ActionType actType )
        {
            if( actType == ActionType.ATTACK || actType == ActionType.HEAL || actType == ActionType.SUPPORT ) { return true; }

            return false;
        }
    }
}