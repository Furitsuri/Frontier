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
            context.StageCtrl.OperateGridCursor( dir );

            var actionRangeCtrl     = context.Owner.BattleLogic.ActionRangeCtrl;
            var targetingRangeIndex = context.StageCtrl.GetCurrentGridIndex();

            // ターゲット指定中のグリッドが攻撃可能なタイルを指していない場合は、ターゲットキャラクターを再設定せずに処理を終了する
            if( !actionRangeCtrl.ActionableTileData.AttackableTileMap.ContainsKey( targetingRangeIndex ) )
            {
                // RefreshTargetableRangeが呼ばれないため、古いターゲット情報をここで明示的にクリアする
                actionRangeCtrl.ActionableTileData.ClearAttackTargetTileIndicies();
                return;
            }

            actionRangeCtrl.RefreshTargetableRange( TargetingMode.PART_OF_RANGE, false, isWithMove, targetingRangeIndex, range );
        }

        static public void RefreshTargetableRange( TargetingRangeContext context, bool isFirstRefresh, bool isWithMove, int tileIndex, int currentRange, ActionableTileData actionableTileData )
        {
            if( isFirstRefresh )
            {
                foreach( var attackableMap in actionableTileData.AttackableTileMap )
                {
                    if( Methods.HasAnyFlag( attackableMap.Value.Flag, TileBitFlag.ATTACKABLE_TARGET_EXIST ) )
                    {
                        actionableTileData.AddAttackTargetTileIndex( attackableMap.Key );
                    }
                }

                // スキル使用者から見て直線状の敵を優先し、いない場合は最も近い敵をターゲットにする
                var targetCharacter = context.BtlRtnCtrl.BtlCharaCdr.GetNearestLineOfSightCharacter( context.Owner, context.Owner.BattleLogic.ActionRangeCtrl.GetAttackTargetCharacterKeys() );
                if( null == targetCharacter )
                {
                    targetCharacter = context.BtlRtnCtrl.BtlCharaCdr.GetNearestCharacter( context.Owner, context.Owner.BattleLogic.ActionRangeCtrl.GetAttackTargetCharacterKeys() );
                }
                Debug.Assert( targetCharacter != null, "攻撃可能なグリッドが存在する場合、必ずターゲットキャラクターが存在するはずですが、nullが返されました。" );
                context.StageCtrl.MoveGridCursorToAttackableTile( targetCharacter );
                tileIndex = context.StageCtrl.GetCurrentGridIndex();

                actionableTileData.ClearAttackTargetTileIndicies();
            }

            context.StageCtrl.TileDataHdlr().BeginExpandTargetableTilesWithPartOfRange( tileIndex, currentRange, context.Owner.GetCharacterTag(), actionableTileData );
        }

        static private bool IsExistOpponent( TileDynamicData tileDynamicData, CHARACTER_TAG ownerTag )
        {
            return tileDynamicData.CharaKey.IsValid() && BattleLogicBase.IsOpponentFaction[( int ) ownerTag]( tileDynamicData.CharaKey.CharacterTag );
        }

        static public void RefreshFocusTarget( TargetingRangeContext context, bool isMovingSkill, ref List<CharacterKey> attackTargetCharaKeys, ref Character targetCharacter )
        {
            var actionRangeCtrl     = context.Owner.BattleLogic.ActionRangeCtrl;
            attackTargetCharaKeys   = actionRangeCtrl.GetAttackTargetCharacterKeys();


            /*
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
            */

            // 攻撃可能なキャラクターが攻撃対象範囲に居る場合、ターゲット選択時グリッドカーソルをそのキャラクターに合わせる
            if( 0 < attackTargetCharaKeys.Count )
            {
                if( null == targetCharacter )
                {
                    targetCharacter = context.BtlRtnCtrl.BtlCharaCdr.GetNearestCharacter( context.Owner, attackTargetCharaKeys );
                    Debug.Assert( targetCharacter != null, "攻撃可能なグリッドが存在する場合、必ずターゲットキャラクターが存在するはずですが、nullが返されました。" );
                    context.StageCtrl.MoveGridCursorToAttackableTile( targetCharacter );
                }
                else if( attackTargetCharaKeys.Contains( targetCharacter.GetCharacterKey() ) )
                {
                    context.StageCtrl.MoveGridCursorToAttackableTile( targetCharacter );
                }
                else
                {
                    // ターゲットしている敵が攻撃対象範囲から外れた場合はターゲットカーソルを非表示にする
                    context.StageCtrl.SetActiveTargetCursor( false );
                }
            }
            else
            {
                targetCharacter = null;
                context.StageCtrl.SetActiveTargetCursor( false );
            }
        }

		static public bool TryAdjustRange( TargetingRangeContext context, int step, ref int currentRange, int maxRange, bool isMovingSkill, ref List<CharacterKey> attackTargets, ref Character targetCharacter )
        {
            return false;
        }
    }
}
