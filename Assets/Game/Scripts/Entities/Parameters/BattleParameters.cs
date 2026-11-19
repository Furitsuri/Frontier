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
        /// 攻撃力の補正値を算出します。装備中のパッシブスキルごとの補正値と、スキルによる補正値の合計です。
        /// MEMO : 倍率を全て合算してから整数化すると、小さな補正(例: 基礎値4の+20%)が他の補正の端数と合わせて切り捨てられ、
        ///        効果が無かったことになる場合があるため、パッシブスキルは1つずつ整数化してから合計します。
        ///        これにより、ステータス画面のツールチップに表示する内訳(GetAtkBreakdown)と合計値が一致します。
        /// 攻撃予測(AttackForecaster)等、実際の補正値と同じ算出方法で試算したい場合にも使用してください。
        /// </summary>
        /// <param name="ownerStatus">対象キャラクターのステータス</param>
        /// <param name="skillAtkMagnification">スキルによる攻撃力倍率の加算値</param>
        static public int CalculateModifiedAtk( in Status ownerStatus, float skillAtkMagnification )
        {
            return CalculatePassiveModifiedValue( ownerStatus, ownerStatus.Atk, PassiveEffectType.ATK_MAGNIFICATION )
                 + CalculateModifiedValue( ownerStatus.Atk, skillAtkMagnification );
        }

        /// <summary>
        /// 防御力の補正値を算出します。装備中のパッシブスキルごとの補正値と、スキルによる補正値の合計です(CalculateModifiedAtkと同様)。
        /// </summary>
        /// <param name="ownerStatus">対象キャラクターのステータス</param>
        /// <param name="skillDefMagnification">スキルによる防御力倍率の加算値</param>
        static public int CalculateModifiedDef( in Status ownerStatus, float skillDefMagnification )
        {
            return CalculatePassiveModifiedValue( ownerStatus, ownerStatus.Def, PassiveEffectType.DEF_MAGNIFICATION )
                 + CalculateModifiedValue( ownerStatus.Def, skillDefMagnification );
        }

        /// <summary>
        /// 装備中のパッシブスキル及び固有スキルのみによる攻撃力の補正値を算出します(使用中のスキルの倍率は含みません)。
        /// 常時の効果であるパッシブスキル分を、一時的なスキル効果と区別して表示する場合などに使用します。
        /// </summary>
        static public int CalculatePassiveModifiedAtk( in Status ownerStatus ) => CalculatePassiveModifiedValue( ownerStatus, ownerStatus.Atk, PassiveEffectType.ATK_MAGNIFICATION );

        /// <summary>
        /// 装備中のパッシブスキル及び固有スキルのみによる防御力の補正値を算出します(使用中のスキルの倍率は含みません)。
        /// </summary>
        static public int CalculatePassiveModifiedDef( in Status ownerStatus ) => CalculatePassiveModifiedValue( ownerStatus, ownerStatus.Def, PassiveEffectType.DEF_MAGNIFICATION );

        /// <summary>
        /// 現在の攻撃力の補正値(ModifiedParam.Atk)の内訳を、要因(装備中のパッシブスキル→固有スキル→適用中のスキルの順)ごとに求めます。
        /// 内訳の合計はModifiedParam.Atkと一致します。
        /// </summary>
        public void GetAtkBreakdown( in Status ownerStatus, List<ModifierBreakdown.Entry> dest )
        {
            CollectPassiveBreakdown( ownerStatus, ownerStatus.Atk, PassiveEffectType.ATK_MAGNIFICATION, dest );
            CollectAppliedSkillBreakdown( ownerStatus.Atk, true, CalculateModifiedValue( ownerStatus.Atk, _skillModifiedParam.AtkMagnification ), dest );
        }

        /// <summary>
        /// 現在の防御力の補正値(ModifiedParam.Def)の内訳を、要因(装備中のパッシブスキル→固有スキル→適用中のスキルの順)ごとに求めます。
        /// 内訳の合計はModifiedParam.Defと一致します。
        /// </summary>
        public void GetDefBreakdown( in Status ownerStatus, List<ModifierBreakdown.Entry> dest )
        {
            CollectPassiveBreakdown( ownerStatus, ownerStatus.Def, PassiveEffectType.DEF_MAGNIFICATION, dest );
            CollectAppliedSkillBreakdown( ownerStatus.Def, false, CalculateModifiedValue( ownerStatus.Def, _skillModifiedParam.DefMagnification ), dest );
        }

        /// <summary>
        /// 装備中のパッシブスキル及び固有スキルによる補正値を、スキル1つずつ整数化して合計します。
        /// </summary>
        static private int CalculatePassiveModifiedValue( in Status ownerStatus, int baseValue, PassiveEffectType effectType )
        {
            int total = 0;

            if( ownerStatus.PassiveSkills != null )
            {
                foreach( var skillID in ownerStatus.PassiveSkills )
                {
                    if( TryGetEquipPassiveMagnification( skillID, effectType, out float magnification ) )
                    {
                        total += CalculateModifiedValue( baseValue, magnification );
                    }
                }
            }

            if( TryGetUniqueMagnification( ownerStatus, effectType, out _, out float uniqueMagnification ) )
            {
                total += CalculateModifiedValue( baseValue, uniqueMagnification );
            }

            return total;
        }

        /// <summary>
        /// 装備中のパッシブスキル及び固有スキルによる補正値の内訳を追加します(装備枠の順→固有スキルの順)。
        /// </summary>
        static private void CollectPassiveBreakdown( in Status ownerStatus, int baseValue, PassiveEffectType effectType, List<ModifierBreakdown.Entry> dest )
        {
            if( ownerStatus.PassiveSkills != null )
            {
                foreach( var skillID in ownerStatus.PassiveSkills )
                {
                    if( TryGetEquipPassiveMagnification( skillID, effectType, out float magnification ) )
                    {
                        dest.Add( new ModifierBreakdown.Entry( PassiveSkillsData.data[( int ) skillID].Name, CalculateModifiedValue( baseValue, magnification ) ) );
                    }
                }
            }

            if( TryGetUniqueMagnification( ownerStatus, effectType, out var uniqueSkillID, out float uniqueMagnification ) )
            {
                dest.Add( new ModifierBreakdown.Entry( UniqueSkillsData.data[( int ) uniqueSkillID].Name, CalculateModifiedValue( baseValue, uniqueMagnification ) ) );
            }
        }

        /// <summary>
        /// 装備枠のパッシブスキルが指定の効果を持つ場合、その倍率を取得します。
        /// TODO : スキル強化の実装時に、装備枠ごとの強化段階を受け取り、その段階の効果量を用いるよう変更すること(現状は全て未強化として扱う)
        /// </summary>
        static private bool TryGetEquipPassiveMagnification( PassiveSkillID skillID, PassiveEffectType effectType, out float magnification )
        {
            magnification = 0f;
            if( !PassiveSkillsData.IsValidSkill( skillID ) ) { return false; }

            return PassiveSkillsData.data[( int ) skillID].Effect.TryGetMagnification( effectType, 0, out magnification );
        }

        /// <summary>
        /// キャラクターの固有スキルが指定の効果を持つ場合、レベルに応じた強化段階での倍率を取得します。
        /// </summary>
        static private bool TryGetUniqueMagnification( in Status ownerStatus, PassiveEffectType effectType, out UniqueSkillID skillID, out float magnification )
        {
            magnification = 0f;
            skillID = UniqueSkillsData.GetSkillID( ownerStatus );
            if( !UniqueSkillsData.IsValidSkill( skillID ) ) { return false; }

            return UniqueSkillsData.data[( int ) skillID].Effect.TryGetMagnification( effectType, UniqueSkillsData.GetGrade( ownerStatus.Level ), out magnification );
        }

        /// <summary>
        /// 適用中のスキルによる補正値(skillTotal)を、スキルごとの倍率に応じて振り分けた内訳を追加します。
        /// MEMO : スキルによる倍率は合算してから整数化している(パリィによる倍率の直接変更もあるため)ので、ModifierBreakdown.Apportionで
        ///        合計が一致するよう振り分けます。パリィによる変更分は要因として扱わず、他のスキルに振り分けられます。
        /// </summary>
        private void CollectAppliedSkillBreakdown( int baseValue, bool isAtk, int skillTotal, List<ModifierBreakdown.Entry> dest )
        {
            var sources = new List<ModifierBreakdown.Source>();
            for( int i = 0; i < ( int ) ActiveSkillID.NUM; ++i )
            {
                if( !IsSkillApplied( ( ActiveSkillID ) i ) ) { continue; }

                var skillData       = ActiveSkillsData.data[i];
                float magnification = isAtk ? skillData.AddAtkMag : skillData.AddDefMag;
                if( magnification == 0f ) { continue; }

                sources.Add( new ModifierBreakdown.Source( skillData.Name, magnification ) );
            }

            ModifierBreakdown.Apportion( baseValue, sources, skillTotal, dest );
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