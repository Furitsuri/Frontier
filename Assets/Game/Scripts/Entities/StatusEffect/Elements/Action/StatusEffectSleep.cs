using System;

namespace Frontier.Entities
{
    public class StatusEffectSleep : StatusEffectActionBase
    {
        public StatusEffectSleep() : base()
        {
            _bitFlag = Convert.ToInt32( ActionStatusEffect.SLEEP );
        }

        /// <summary>
        /// キャラクターの各タイルの移動コストを浮遊状態用に変更する
        /// </summary>
        public override void ApplyEffect()
        {
        }
    }
}