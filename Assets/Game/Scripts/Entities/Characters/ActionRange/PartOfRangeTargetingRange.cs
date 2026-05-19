using System;
using System.Collections.Generic;
using Frontier.Battle;
using Frontier.Combat;
using Frontier.Stage;
using Frontier.UI;
using UnityEngine;

namespace Frontier.Entities
{
    public static class PartOfRangeTargetingRange
    {
        static public void AcceptDirection( TargetingRangeContext context, Direction dir, bool isWithMove, int range )
        {
            context.StageCtrl.OperateGridCursorController( dir );

            var actionRangeCtrl     = context.Owner.BattleLogic.ActionRangeCtrl;
            var targetingRangeIndex = context.StageCtrl.GetCurrentGridIndex();

            // ターゲット指定中のグリッドが攻撃可能なタイルを指していない場合は、ターゲットキャラクターを再設定せずに処理を終了する
            if( !actionRangeCtrl.ActionableTileData.AttackableTileMap.ContainsKey( targetingRangeIndex ) )
            {
                return;
            }

            actionRangeCtrl.RefreshTargetableRange( TargetingMode.PART_OF_RANGE, isWithMove, targetingRangeIndex, range );
        }

        static public void RefreshTargetableRange( TargetingRangeContext context, bool isWithMove, int tileIndex, int currentRange, ActionableTileData actionableTileData )
        {
            context.StageCtrl.TileDataHdlr().BeginExpandTargetableTilesWithPartOfRange( tileIndex, currentRange, context.Owner.GetCharacterTag(), actionableTileData );
        }

        static public void RefreshFocusTarget( TargetingRangeContext context, ref List<CharacterKey> attackTargetCharaKeys, ref Character targetCharacter, bool isMovingSkill )
        {
            var actionRangeCtrl     = context.Owner.BattleLogic.ActionRangeCtrl;
            attackTargetCharaKeys   = actionRangeCtrl.GetAttackTargetCharacterKeys();

            // 攻撃可能なグリッド内に敵がおり、尚且つ現在のターゲットキャラクターが存在しない場合は、ターゲットキャラクターを設定する
            if( 0 < attackTargetCharaKeys.Count )
            {
                if( null == targetCharacter )
                {
                    targetCharacter = context.BtlRtnCtrl.BtlCharaCdr.GetNearestCharacter( context.Owner, attackTargetCharaKeys );
                    Debug.Assert( targetCharacter != null, "攻撃可能なグリッドが存在する場合、必ずターゲットキャラクターが存在するはずですが、nullが返されました。" );
                    context.StageCtrl.MoveGridCursorToAttackableTile( targetCharacter );
                }
                else
                {
                    // 現在のターゲットキャラクターが攻撃可能なグリッド内にいる場合は、ターゲットキャラクターを再設定しない
                    if(!attackTargetCharaKeys.Contains( targetCharacter.GetCharacterKey() ))
                    {
                        targetCharacter = context.BtlRtnCtrl.BtlCharaCdr.GetNearestCharacter( context.Owner, attackTargetCharaKeys );
                        Debug.Assert( targetCharacter != null, "攻撃可能なグリッドが存在する場合、必ずターゲットキャラクターが存在するはずですが、nullが返されました。" );
                        context.StageCtrl.MoveGridCursorToAttackableTile( targetCharacter );
                    }
                }
            }
            else
            {
                targetCharacter = null;
            }

            context.Presenter.SetActiveActionResultExpect( targetCharacter != null, ParameterWindowType.Left );
        }

		static public bool TryAdjustRange( TargetingRangeContext context, int step, ref int currentRange, int maxRange, bool isMovingSkill, ref List<CharacterKey> attackTargets, ref Character targetCharacter )
        {
            return false;
        }
    }
}
