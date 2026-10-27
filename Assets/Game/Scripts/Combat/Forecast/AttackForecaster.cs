using Frontier.Entities;
using UnityEngine;

namespace Frontier.Combat
{
    /// <summary>
    /// 攻撃を行った場合の結果を予測します。
    /// キャラクターの状態を一切書き換えないため、AIの思考などで任意の攻撃を試算する用途にも使用出来ます。
    /// </summary>
    static public class AttackForecaster
    {
        /// <summary>
        /// attacker が target を攻撃した場合の結果を予測します。
        /// useSkillID を指定した場合は、現在の状態に加えてそのスキルを使用したものとして予測します
        /// (既に適用中のスキルを指定した場合は、二重には加算しません)。
        /// MEMO : 防御側が攻撃を受ける際に使用するスキル(ガード等)は、適用済みのものだけが反映されます
        /// </summary>
        /// <param name="attacker">攻撃キャラクター</param>
        /// <param name="target">標的キャラクター</param>
        /// <param name="useSkillID">攻撃時に使用するスキル。SkillID.NONEの場合は現在の状態のまま予測します</param>
        /// <returns>攻撃結果の予測</returns>
        static public AttackForecast Forecast( Character attacker, Character target, SkillID useSkillID = SkillID.NONE )
        {
            var attackerParams      = attacker.BattleParams;
            var skillModified       = attackerParams.SkillModifiedParam;
            int modifiedAtk         = attackerParams.ModifiedParam.Atk;
            int actGaugeCost        = attackerParams.TmpParam.ActGaugeConsumption;

            if( SkillsData.IsValidSkill( useSkillID ) && !attackerParams.IsSkillApplied( useSkillID ) )
            {
                var skillData = SkillsData.data[( int ) useSkillID];

                skillModified.AddAtkNum         += skillData.AddAtkNum;
                skillModified.AtkMagnification  += skillData.AddAtkMag;
                // BattleParameters.RegreshModifiedParamと同じ算出方法で補正値を求める
                modifiedAtk                     = ( int ) ( ( float ) attacker.GetStatusRef.Atk * skillModified.AtkMagnification );
                actGaugeCost                    += skillData.Cost;
            }

            int targetDef   = target.GetStatusRef.Def + target.BattleParams.ModifiedParam.Def;
            int attackerAtk = attacker.GetStatusRef.Atk + modifiedAtk;
            int damage      = attackerAtk - targetDef;

            var forecast                = new AttackForecast();
            forecast.SingleDamage       = Mathf.Max( damage, 0 );
            forecast.TotalDamage        = Mathf.Max( damage * ( 1 + skillModified.AddAtkNum ), 0 );
            forecast.TargetCurrentHp    = target.GetStatusRef.CurHP;
            forecast.IsDefeat           = forecast.TargetCurrentHp <= forecast.TotalDamage;
            forecast.ActionGaugeCost    = actGaugeCost;
            forecast.SelfStatusEffects  = AttackForecast.EmptyStatusEffects;

            return forecast;
        }
    }
}
