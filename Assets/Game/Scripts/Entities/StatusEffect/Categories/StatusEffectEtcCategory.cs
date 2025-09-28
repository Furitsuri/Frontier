using System;
using static Constants;

namespace Frontier.Entities
{
    /// <summary>
    /// 
    /// </summary>
    public class StatusEffectEtcCategory : StatusEffectCategoryBase
    {
        public StatusEffectEtcCategory() : base()
        {
            _additionalBitByCategory = STATUS_EFFECT_CATEGORY_BIT * Convert.ToInt32( StatusEffectCategory.Etc );

            _elementFactorys = new Func<StatusEffectElementBase>[( int ) EtcStatusEffect.NUM]
            {
                () => _hierarchyBld.InstantiateWithDiContainer<StatusEffectInvisible>(false)
            };
        }
    }
}