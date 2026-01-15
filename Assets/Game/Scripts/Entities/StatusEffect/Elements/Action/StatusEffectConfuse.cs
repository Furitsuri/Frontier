using System;

namespace Frontier.Entities
{
    public class StatusEffectConfuse : StatusEffectActionBase
    {
        public StatusEffectConfuse() : base()
        {
            _bitFlag = Convert.ToInt32( ActionStatusEffect.CONFUSE );
        }

        /// <summary>
        /// キャラクターの各タイルの移動コストを浮遊状態用に変更する
        /// </summary>
        public override void ApplyEffect()
        {
        }
    }
}