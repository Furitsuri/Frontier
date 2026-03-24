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

            RegreshModifiedParam( ownerStatus );
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

            RegreshModifiedParam( ownerStatus );
        }

        public void RegreshModifiedParam( in Status ownerStatus )
        {
            float atk  = ( float ) ownerStatus.Atk * _skillModifiedParam.AtkMagnification;
            float def  = ( float ) ownerStatus.Def * _skillModifiedParam.DefMagnification;

            var absAtk = Mathf.Abs( _modifiedParam.Atk );
            if( 0f < absAtk && absAtk < 1f )
            {
                _modifiedParam.Atk = ( int )( 1f * ( ( atk < 0f ) ? -1f : 1f ) );
            }
            else
            {
                _modifiedParam.Atk = ( int ) atk;
            }

                var absDef = Mathf.Abs( _modifiedParam.Def );
            if( 0f < absDef && absDef < 1f )
            {
                _modifiedParam.Def = ( int ) ( 1f * ( ( def < 0f ) ? -1f : 1f ) );
            }
            else
            {
                _modifiedParam.Def = ( int ) def;
            }
        }
    }
}