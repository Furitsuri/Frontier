using System;
using UnityEngine;
using static Constants;

namespace Frontier.Entities
{
    /// <summary>
    /// 
    /// </summary>
    public class StatusEffectMoveCategory : StatusEffectCategoryBase
    {
        public StatusEffectMoveCategory() : base()
        {
            _additionalBitByCategory = STATUS_EFFECT_CATEGORY_BIT * Convert.ToInt32( StatusEffectCategory.Move );

            _elementFactorys = new Func<StatusEffectElementBase>[( int ) MoveStatusEffect.NUM]
            {
                () => _hierarchyBld.InstantiateWithDiContainer<StatusEffectFloating>(false),        // ActionStatusEffect.SLEEP
                () => _hierarchyBld.InstantiateWithDiContainer<StatusEffectHyperGravity>(false)     // ActionStatusEffect.CONFUSE
            };
        }
    }
}