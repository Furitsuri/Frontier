using System;
using UnityEngine;
using static Constants;

namespace Frontier.Entities
{
    public class StatusEffectPoison : StatusEffectParameterBase
    {
        public StatusEffectPoison() : base()
        {
            _bitFlag = Convert.ToInt32( ParameterStatusEffect.POISON );
        }

        /// <summary>
        /// 毒のダメージを適用します
        /// </summary>
        public override void ApplyEffect()
        {
            int poisonDamage = (int)Mathf.Ceil(_targetCharacter.Params.CharacterParam.MaxHP * POISON_DAMAGE_RATE ); // 小数点切り上げ
            _targetCharacter.Params.CharacterParam.AddDamage( poisonDamage );
        }
    }
}