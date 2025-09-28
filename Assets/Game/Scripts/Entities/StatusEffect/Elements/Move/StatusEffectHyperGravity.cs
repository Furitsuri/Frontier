using Frontier.Stage;
using System;

namespace Frontier.Entities
{
    public class StatusEffectHyperGravity : StatusEffectMoveBase
    {
        public StatusEffectHyperGravity() : base()
        {
            _bitFlag = Convert.ToInt32( MoveStatusEffect.HYPER_GRAVITY );
        }

        /// <summary>
        /// キャラクターの各タイルの移動コストを過重力状態用に変更する
        /// </summary>
        override public void ApplyEffect()
        {
            _targetCharacter.ApplyCostTable( TileCostTables.HyperGravityCostTable );
        }
    }
}