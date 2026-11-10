using Frontier.Entities;
using UnityEngine;

namespace Frontier.Combat
{
    /// <summary>
    /// 回復系のスキルを使用した場合の回復量を予測します。
    /// AttackForecasterと同様、キャラクターの状態を一切書き換えません。
    /// </summary>
    static public class HealForecaster
    {
        /// <summary>
        /// healer が target に回復系のスキルを使用した場合の、実際に回復するHP量を予測します。
        /// 最大HPを超えて回復することは無いため、スキルの回復量と最大HPまでの不足分のうち、小さい方となります
        /// </summary>
        /// <param name="healer">スキルの使用者</param>
        /// <param name="target">回復の対象</param>
        /// <param name="useSkillID">使用する回復系のスキル</param>
        /// <returns>回復するHP量(0以上)</returns>
        static public int Forecast( Character healer, Character target, SkillID useSkillID )
        {
            if( !SkillsData.IsValidSkill( useSkillID ) ) { return 0; }

            var status      = target.GetStatusRef;
            int lackingHp   = Mathf.Max( status.MaxHP - status.CurHP, 0 );

            return Mathf.Min( SkillsData.data[( int ) useSkillID].HealAmount, lackingHp );
        }
    }
}
