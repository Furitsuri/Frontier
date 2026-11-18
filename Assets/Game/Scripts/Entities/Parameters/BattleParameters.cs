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
        public bool IsSkillApplied( ActiveSkillID skillID )
        {
            if( !ActiveSkillsData.IsValidSkill( skillID ) ) { return false; }

            return 0 != ( _appliedSkillBit & Methods.ToBit64( skillID ) );
        }

        public void ApplyStatusEffect( StatusEffect statusEff )
        {
            _appliedStarusEffects.Add( statusEff );
        }

        public void ApplySkill( ActiveSkillID skillID, in Status ownerStatus )
        {
            if( !ActiveSkillsData.IsValidSkill( skillID ) ) { return; }
            var skillData = ActiveSkillsData.data[( int ) skillID];

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

        public void RemoveSkill( ActiveSkillID skillID, in Status ownerStatus )
        {
            if( !ActiveSkillsData.IsValidSkill( skillID ) ) { return; }
            var skillData = ActiveSkillsData.data[( int ) skillID];

            _tmpParam.ActGaugeConsumption           -= skillData.Cost;
            _skillModifiedParam.AddAtkNum           -= skillData.AddAtkNum;
            _skillModifiedParam.AtkMagnification    -= skillData.AddAtkMag;
            _skillModifiedParam.DefMagnification    -= skillData.AddDefMag;

            Methods.UnsetBitFlag( ref _appliedSkillBit, skillID );

            RefreshModifiedParam( ownerStatus );
        }

        /// <summary>
        /// 適用中の全スキルの効果(攻撃回数・倍率、及びパリィ等による倍率の直接変更分)を取り除きます。
        /// スキルの効果は戦闘1回限りのため、攻撃シーケンスの終了時やフェーズの終了時に呼び出してください。
        /// MEMO : アクションゲージ消費量はスキルの使用確定時に消費済み(0にリセット済み)であるため、ここでは扱いません。
        /// </summary>
        /// <param name="ownerStatus">所有キャラクターのステータス</param>
        public void ResetAppliedSkills( in Status ownerStatus )
        {
            _skillModifiedParam.Init();
            _appliedSkillBit = 0;

            RefreshModifiedParam( ownerStatus );
        }

        /// <summary>
        /// 補正値(ModifiedParam)を、適用中のスキルの倍率と装備中のパッシブスキルの倍率から算出し直します。
        /// MEMO : パッシブスキルの倍率は保持せず、ここで毎回ステータスの装備内容から求めます。
        ///        そのため攻撃ごとのスキル効果のリセット(ResetAppliedSkills)でも消えず、装備の変更も常に反映されます。
        /// </summary>
        public void RefreshModifiedParam( in Status ownerStatus )
        {
            _modifiedParam.Atk = CalculateModifiedAtk( ownerStatus, _skillModifiedParam.AtkMagnification );
            _modifiedParam.Def = CalculateModifiedDef( ownerStatus, _skillModifiedParam.DefMagnification );
        }

        /// <summary>
        /// 攻撃力の補正値を算出します。スキルによる倍率に、装備中のパッシブスキルによる倍率を加算して求めます。
        /// 攻撃予測(AttackForecaster)等、実際の補正値と同じ算出方法で試算したい場合にも使用してください。
        /// </summary>
        /// <param name="ownerStatus">対象キャラクターのステータス</param>
        /// <param name="skillAtkMagnification">スキルによる攻撃力倍率の加算値</param>
        static public int CalculateModifiedAtk( in Status ownerStatus, float skillAtkMagnification )
        {
            float passiveMagnification = PassiveSkillsData.SumEffectValue( ownerStatus.PassiveSkills, PassiveEffectType.ATK_MAGNIFICATION );

            return CalculateModifiedValue( ownerStatus.Atk, skillAtkMagnification + passiveMagnification );
        }

        /// <summary>
        /// 装備中のパッシブスキルのみによる攻撃力の補正値を算出します(使用中のスキルの倍率は含みません)。
        /// 常時の効果であるパッシブスキル分を、一時的なスキル効果と区別して表示する場合などに使用します。
        /// </summary>
        static public int CalculatePassiveModifiedAtk( in Status ownerStatus ) => CalculateModifiedAtk( ownerStatus, 0f );

        /// <summary>
        /// 装備中のパッシブスキルのみによる防御力の補正値を算出します(使用中のスキルの倍率は含みません)。
        /// </summary>
        static public int CalculatePassiveModifiedDef( in Status ownerStatus ) => CalculateModifiedDef( ownerStatus, 0f );

        /// <summary>
        /// 防御力の補正値を算出します。スキルによる倍率に、装備中のパッシブスキルによる倍率を加算して求めます。
        /// </summary>
        /// <param name="ownerStatus">対象キャラクターのステータス</param>
        /// <param name="skillDefMagnification">スキルによる防御力倍率の加算値</param>
        static public int CalculateModifiedDef( in Status ownerStatus, float skillDefMagnification )
        {
            float passiveMagnification = PassiveSkillsData.SumEffectValue( ownerStatus.PassiveSkills, PassiveEffectType.DEF_MAGNIFICATION );

            return CalculateModifiedValue( ownerStatus.Def, skillDefMagnification + passiveMagnification );
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