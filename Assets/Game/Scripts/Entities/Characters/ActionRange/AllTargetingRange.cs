using System;
using System.Collections.Generic;
using Frontier.Battle;
using Frontier.Stage;

namespace Frontier.Entities
{
    public static class AllTargetingRange
    {
        /// <summary>
        /// 全てのタイルを攻撃対象にする場合は、方向転換しても攻撃対象は変わらないため何もしません。
        /// </summary>
        /// <param name="context"></param>
        /// <param name="dir"></param>
        /// <param name="isWithMove"></param>
        /// <param name="range"></param>
        /// <param name="attackTargets"></param>
        /// <param name="targetCharacter"></param>
        static public void AcceptDirection( TargetingRangeContext context, Direction dir, bool isWithMove, ref int currentRange, int maxRange )
        {
        }

        static public void RefreshTargetableRange( TargetingRangeContext context, bool isFirstRefresh, bool isWithMove, int tileIndex, int currentRange, ActionableTileData actionableTileData )
        {
            foreach( var attackableMap in actionableTileData.AttackableTileMap )
            {
                actionableTileData.AddTargetableTile( attackableMap.Key, attackableMap.Value );
            }
        }

        static public void RefreshFocusTarget( TargetingRangeContext context, bool isMovingSkill, ref List<CharacterKey> attackTargetCharaKeys, ref Character targetCharacter )
        {
        }

        static public bool TryAdjustRange( TargetingRangeContext context, int step, ref int currentRange, int maxRange, bool isMovingSkill, ref List<CharacterKey> attackTargets, ref Character targetCharacter )
        {
            return false;
        }

        static private bool IsExistOpponent( TileDynamicData tileDynamicData, CHARACTER_TAG ownerTag )
        {
            return tileDynamicData.CharaKey.IsValid() && BattleLogicBase.IsOpponentFaction[( int ) ownerTag]( tileDynamicData.CharaKey.CharacterTag );
        }
    }
}
