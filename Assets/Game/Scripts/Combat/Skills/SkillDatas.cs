using Frontier.Entities;
using System;
using System.Collections.Generic;
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
            public RangeShape RangeShape;           // レンジタイプ
            public int RangeValue;                  // レンジ範囲
            public bool IsAdjustableRange;          // レンジ調節可否
            public TargetingMode TargetingMode;     // レンジ内選択モード
            public int TargetingRange;              // レンジ内選択モードに用いる範囲値
            public bool IsMovingSkill;              // 移動を伴うスキルか否か
            public bool IsCooperative;              // 連携コマンドとして使用可能か
            public float AddAtkMag;                 // 攻撃力倍率加算値
            public float AddDefMag;                 // 防御力倍率加算値
            public int AddAtkNum;                   // 攻撃回数加算値
            public float Param1;                    // 汎用パラメータ1
            public float Param2;                    // 汎用パラメータ2
            public float Param3;                    // 汎用パラメータ3
            public float Param4;                    // 汎用パラメータ4
            public string ExplainTextKey;           // スキル説明文のテキストキー
        }

        static public Data[] data = new Data[( int ) SkillID.NUM];
        // MEMO : 攻撃を受けた際に反応するパリィのような「リアクション型」のスキルのみを登録します。
        //        大半のスキルはリアクションを必要としないため、ここには登録しません(登録が無ければ通知処理自体が存在しません)。
        static public Dictionary<SkillID, Func<SkillNotifierBase>> ReactiveSkillNotifierFactory = null;
        static private readonly SkillActionBase sharedAction            = new SkillActionBase(null);    // 使いまわし前提のため静的読み取り専用

        static public void BuildSkillNotifierFactory( HierarchyBuilderBase hierarchyBld )
        {
            if( ReactiveSkillNotifierFactory != null ) { return; }

            ReactiveSkillNotifierFactory = new Dictionary<SkillID, Func<SkillNotifierBase>>
            {
                { SkillID.PARRY, () => hierarchyBld.InstantiateWithDiContainer<ParrySkillNotifier>(false) },
            };
        }

        static public SkillActionBase CreateSkillAction( SkillID skillID, Character owner, List<CharacterKey> targetCharaKeys, HierarchyBuilderBase hierarchyBld )
        {
            switch( skillID )
            {
                case SkillID.DOUBLE_STRIKE:
                {
                    object[] args = { owner, targetCharaKeys };
                    return hierarchyBld.InstantiateWithDiContainer<PartOfRangeSABase>( args, false );
                }
                case SkillID.DASH_SLASH:
                {
                    object[] args = { owner, targetCharaKeys };
                    return hierarchyBld.InstantiateWithDiContainer<DashSlashSA>( args, false );
                }
                case SkillID.JUMP_SLASH:
                {
                    object[] args = { owner, targetCharaKeys };
                    return hierarchyBld.InstantiateWithDiContainer<JumpSlashSA>( args, false );
                }

                default:
                    return sharedAction;
            }
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