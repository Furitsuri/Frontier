using Frontier.Combat;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Constants;

namespace Frontier.Entities
{
    [Serializable]
    public class BattleParameters
    {
        [SerializeField] private ModifiedParameter _modifiedParam;
        [SerializeField] private SkillModifiedParameter _skillModifiedParam;

        private TemporaryParameter _tmpParam;

        private List<StatusEffect> _appliedStarusEffects    = new List<StatusEffect>();
        private Int64 _appliedSkillBit = 0;

        public ref TemporaryParameter TmpParam => ref _tmpParam;
        public ref ModifiedParameter ModifiedParam => ref _modifiedParam;
        public ref SkillModifiedParameter SkillModifiedParam => ref _skillModifiedParam;

        public void Setup()
        {
            _tmpParam.Setup();
        }

        public void Init()
        {
            _tmpParam.Init();
            _modifiedParam.Init();
            _skillModifiedParam.Init();

            _appliedSkillBit = 0;

            _appliedStarusEffects.Clear();
        }

        /// <summary>
        /// 指定のスキルが適用中であるかを取得します
        /// </summary>
        /// <param name="skillID">指定スキルID</param>
        /// <returns>適用中か否か</returns>
        public bool IsSkillApplied( SkillID skillID )
        {
            if( !SkillsData.IsValidSkill( skillID ) ) { return false; }

            return 0 != ( _appliedSkillBit & Methods.ToBit64( skillID ) );
        }

        public void ApplyStatusEffect( StatusEffect statusEff )
        {
            _appliedStarusEffects.Add( statusEff );
        }

        public void ApplySkill( SkillID skillID, in Status ownerStatus )
        {
            if( !SkillsData.IsValidSkill( skillID ) ) { return; }
            var skillData = SkillsData.data[( int ) skillID];

            _tmpParam.ActGaugeConsumption           += skillData.Cost;
            _skillModifiedParam.AddAtkNum           += skillData.AddAtkNum;
            _skillModifiedParam.AtkMagnification    += skillData.AddAtkMag;
            _skillModifiedParam.DefMagnification    += skillData.AddDefMag;

            Methods.SetBitFlag( ref _appliedSkillBit, skillID );

            RefreshModifiedParam( ownerStatus );
        }

        public void RemoveStatusEffect( StatusEffect statusEff )
        {
            _appliedStarusEffects.Remove( statusEff );
        }

        public void RemoveSkill( SkillID skillID, in Status ownerStatus )
        {
            if( !SkillsData.IsValidSkill( skillID ) ) { return; }
            var skillData = SkillsData.data[( int ) skillID];

            _tmpParam.ActGaugeConsumption           -= skillData.Cost;
            _skillModifiedParam.AddAtkNum           -= skillData.AddAtkNum;
            _skillModifiedParam.AtkMagnification    -= skillData.AddAtkMag;
            _skillModifiedParam.DefMagnification    -= skillData.AddDefMag;

            Methods.UnsetBitFlag( ref _appliedSkillBit, skillID );

            RefreshModifiedParam( ownerStatus );
        }

        public void RefreshModifiedParam( in Status ownerStatus )
        {
            _modifiedParam.Atk = CalculateModifiedValue( ownerStatus.Atk, _skillModifiedParam.AtkMagnification );
            _modifiedParam.Def = CalculateModifiedValue( ownerStatus.Def, _skillModifiedParam.DefMagnification );
        }

        /// <summary>
        /// 基礎値に倍率を掛けた補正値を求めます。
        /// 補正値の絶対値が1未満(0は除く)となる場合は、補正が切り捨てによって消えてしまわないよう、符号を保ったまま1とします
        /// </summary>
        /// <param name="baseValue">基礎値</param>
        /// <param name="magnification">倍率</param>
        /// <returns>補正値</returns>
        static public int CalculateModifiedValue( int baseValue, float magnification )
        {
            float value     = ( float ) baseValue * magnification;
            float absValue  = Mathf.Abs( value );

            if( 0f < absValue && absValue < 1f )
            {
                return ( value < 0f ) ? -1 : 1;
            }

            return ( int ) value;
        }
    }
}