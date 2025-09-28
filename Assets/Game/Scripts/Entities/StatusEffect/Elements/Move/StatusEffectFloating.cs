using Frontier.Stage;
using System;

namespace Frontier.Entities
{
    public class StatusEffectFloating : StatusEffectMoveBase
    {
        public StatusEffectFloating() : base()
        {
            _bitFlag = Convert.ToInt32( MoveStatusEffect.FLOATING );
        }

        /// <summary>
        /// キャラクターの各タイルの移動コストを浮遊状態用に変更する
        /// </summary>
        override public void ApplyEffect()
        {
            _targetCharacter.ApplyCostTable( TileCostTables.floatingCostTable );
        }
    }
}