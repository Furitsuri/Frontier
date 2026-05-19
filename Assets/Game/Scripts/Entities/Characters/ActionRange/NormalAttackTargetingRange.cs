using System;
using System.Collections.Generic;
using Frontier.Battle;
using Frontier.Stage;

namespace Frontier.Entities
{
    public static class NormalAttackTargetingRange
    {
        /// <summary>
        /// 通常攻撃は方向転換した際の挙動がDirectionalTargetingRangeの処理と変わらないため、そのまま呼び出しています。
        /// </summary>
        /// <param name="context"></param>
        /// <param name="dir"></param>
        /// <param name="isWithMove"></param>
        /// <param name="range"></param>
        /// <param name="attackTargets"></param>
        /// <param name="targetCharacter"></param>
        static public void AcceptDirection( TargetingRangeContext context, Direction dir, bool isWithMove, int range )
        {
            DirectionalTargetingRange.AcceptDirection( context, dir, isWithMove, range );
        }

        /// <summary>
        /// DirectionalTargetingRangeと同様の挙動のため、そのまま呼び出しています。
        /// </summary>
        /// <param name="context"></param>
        /// <param name="attackTargetCharaKeys"></param>
        /// <param name="targetCharacter"></param>
        /// <param name="isMovingSkill"></param>
        /// <param name="refreshGhostCallback"></param>
        static public void RefreshFocusTarget( TargetingRangeContext context, ref List<CharacterKey> attackTargetCharaKeys, ref Character targetCharacter, bool isMovingSkill, Action<ActionRangeController> refreshGhostCallback )
        {
            DirectionalTargetingRange.RefreshFocusTarget( context, ref attackTargetCharaKeys, ref targetCharacter, isMovingSkill, refreshGhostCallback );
        }

        static public void RefreshTargetableRange( TargetingRangeContext context, bool isWithMove, int tileIndex, int currentRange, ActionableTileData actionableTileData )
        {
            foreach( var attackableMap in actionableTileData.AttackableTileMap )
            {
                if( Methods.HasAnyFlag( attackableMap.Value.Flag, TileBitFlag.ATTACKABLE_TARGET_EXIST ) )
                {
                    actionableTileData.AddAttackTargetTileIndex( attackableMap.Key );
                }
            }
        }

        static public bool TryAdjustRange( TargetingRangeContext context, int step, ref int currentRange, int maxRange, bool isMovingSkill, ref List<CharacterKey> attackTargets, ref Character targetCharacter )
        {
            return false;
        }

        
    }
}
