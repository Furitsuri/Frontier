using System;
using static Constants;

namespace Frontier.Entities
{
    /// <summary>
    /// 
    /// </summary>
    public class StatusEffectActionCategory : StatusEffectCategoryBase
    {
        public StatusEffectActionCategory() : base()
        {
            _additionalBitByCategory = STATUS_EFFECT_CATEGORY_BIT * Convert.ToInt32( StatusEffectCategory.Action );

            _elementFactorys = new Func<StatusEffectElementBase>[( int ) ActionStatusEffect.NUM]
            {
                () => _hierarchyBld.InstantiateWithDiContainer<StatusEffectSleep>(false),   // ActionStatusEffect.SLEEP
                () => _hierarchyBld.InstantiateWithDiContainer<StatusEffectConfuse>(false)  // ActionStatusEffect.CONFUSE
            };
        }
    }
}