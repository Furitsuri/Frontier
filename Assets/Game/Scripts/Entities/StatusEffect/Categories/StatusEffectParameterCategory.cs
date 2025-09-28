using System;
using static Constants;

namespace Frontier.Entities
{
    /// <summary>
    /// 
    /// </summary>
    public class StatusEffectParameterCategory : StatusEffectCategoryBase
    {
        public StatusEffectParameterCategory() : base()
        {
            _additionalBitByCategory = STATUS_EFFECT_CATEGORY_BIT * Convert.ToInt32( StatusEffectCategory.Parameter );

            _elementFactorys = new Func<StatusEffectElementBase>[( int ) ParameterStatusEffect.NUM]
            {
                () => _hierarchyBld.InstantiateWithDiContainer<StatusEffectPoison>(false)
            };
        }
    }
}