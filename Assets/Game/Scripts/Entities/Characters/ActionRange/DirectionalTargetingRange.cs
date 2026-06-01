using System;
using System.Collections.Generic;
using System.Linq;
using Frontier.Combat;
using Frontier.Stage;
using Frontier.UI;
using UnityEngine;

namespace Frontier.Entities
{
    public static class DirectionalTargetingRange
    {
        static public void AcceptDirection( TargetingRangeContext context, Direction dir, bool isMovingSkill, ref int currentRange, int maxRange )
        {
            var actionRangeCtrl = context.Owner.BattleLogic.ActionRangeCtrl;
            currentRange = maxRange;    // 方向指定を受け入れたタイミングでは最大レンジに設定しておき、以降の調整で有効なレンジを探す

            context.Owner.GetTransformHandler.OrderRotate( Quaternion.Euler( 0f, StageDirectionConverter.DirectionAngles[( int ) dir], 0f ) );
            actionRangeCtrl.RefreshTargetableRange( TargetingMode.DIRECTIONAL, false, isMovingSkill, context.Owner.BattleParams.TmpParam.CurrentTileIndex, currentRange );
        }

		static public void RefreshTargetableRange( TargetingRangeContext context, bool isFirstRefresh, bool isMovingSkill, int tileIndex, int range, ActionableTileData actionableTileData )
		{
			Vector3 basePos = context.StageCtrl.GetTileStaticData( context.Owner.BattleParams.TmpParam.CurrentTileIndex ).CharaStandPos;
			Vector3 baseForward = context.Owner.GetTransformHandler.GetOrderedForward();
			baseForward.y = 0f;
			baseForward = baseForward.normalized;

            foreach( var attackableMap in actionableTileData.AttackableTileMap )
            {
				// キャラクターの向きに沿ったタイル
				var targetTilePos = context.StageCtrl.GetTileStaticData( attackableMap.Key ).CharaStandPos;
				if(!Methods.IsMatchForward( baseForward, basePos, targetTilePos )) { continue; }

				// 指定範囲内のタイル
				var tileRange = context.StageCtrl.CalculateTotalRange( tileIndex, attackableMap.Key );
				if( range < tileRange ) { continue; }

				// ターゲット可能タイルとして登録する
				actionableTileData.AddTargetableTile( attackableMap.Key, attackableMap.Value );
                // さらに、そのタイルが攻撃対象として有効な場合は攻撃対象タイルとして追加する
                // ATTACKABLE_TARGET_EXISTはRegisterAttackableTileで敵の存在時に立てられ、
                // DashSlash等の固有フィルタで着地不可な位置の敵については事前に降ろされている
                if( Methods.HasAnyFlag( attackableMap.Value.Flag, TileBitFlag.ATTACKABLE_TARGET_EXIST ) )
				{
					actionableTileData.AddAttackTargetTileIndex( attackableMap.Key );
				}
			}

			// 移動を伴う場合は、最も遠方の移動可能(キャラクターが存在しない)なタイルにターゲット可能レンジを合わせる
			// また、全てのタイルが移動不可、もしくは攻撃対象が存在しない場合は実行不可とする
			if( isMovingSkill )
			{
                // 攻撃対象が存在するタイルがない場合には実行不可
                if( actionableTileData.RefAttackTargetTileIndicies.Count <= 0 )
                {
                    RefreshGhostPreview( context, -1 );
                    return;
                }

				AdjustRangeForMove( context, tileIndex, actionableTileData );
			}
		}

		static public void RefreshFocusTarget( TargetingRangeContext context, bool isMovingSkill, ref List<CharacterKey> attackTargetCharaKeys, ref Character targetCharacter )
        {
            var actionRangeCtrl   = context.Owner.BattleLogic.ActionRangeCtrl;
            attackTargetCharaKeys = actionRangeCtrl.GetAttackTargetCharacterKeys();

            // 攻撃可能なグリッド内に敵がいた場合に標的グリッドを合わせる
            if( 0 < attackTargetCharaKeys.Count )
            {
                // 攻撃対象にしているキャラクターが更新された対象範囲に含まれている場合は何もしない
                if( null != targetCharacter && attackTargetCharaKeys.Contains( targetCharacter.GetCharacterKey() ) )
                {
                    return;
                }

                targetCharacter = context.BtlRtnCtrl.BtlCharaCdr.GetNearestCharacter( context.Owner, attackTargetCharaKeys );
                Debug.Assert( targetCharacter != null, "攻撃可能なグリッドが存在する場合、必ずターゲットキャラクターが存在するはずですが、nullが返されました。" );

                context.StageCtrl.ApplyTargetCursor2CharacterTile( true, targetCharacter );
                context.BtlRtnCtrl.BtlCharaCdr.ApplyDamageExpect( context.Owner, targetCharacter );
                context.Presenter.SetActiveActionResultExpect( true, ParameterWindowType.Left );
            }
            else
            {
                targetCharacter = null;

                // 攻撃可能なグリッドがない場合はカーソル位置(カメラ位置)をプレイヤーに合わせる
                context.StageCtrl.ApplyGridCursor2CharacterTileWithFocusCamera( context.Owner );
                // ターゲットキャラクターがいないため、ターゲットカーソル及び攻撃結果の表示はオフにする
                context.StageCtrl.SetActiveTargetCursor( false );
                context.Presenter.SetActiveActionResultExpect( false, ParameterWindowType.Left );
            }
        }

        /// <summary>
        /// step方向にcurrentRangeを調整します。ゴーストが敵位置と重なるレンジは飛ばし、有効なレンジが見つかれば更新します。
        /// 有効なレンジが存在しない場合は元の状態を復元してfalseを返します。
        /// 拡大時(step>0)はゴーストが現在より遠くなるレンジのみ有効とし、縮小時(step<0)は非敵タイルが1つでもあれば有効とします。
        /// </summary>
        static public bool TryAdjustRange( TargetingRangeContext context, int step, ref int currentRange, int maxRange, bool isMovingSkill, ref List<CharacterKey> attackTargets, ref Character targetCharacter )
        {
            var actionRangeCtrl = context.Owner.BattleLogic.ActionRangeCtrl;
            int tileIndex       = context.Owner.BattleParams.TmpParam.CurrentTileIndex;

            int limit           = ( step < 0 ) ? 1 : maxRange;
            int candidate       = currentRange + step;
            int currentGhostIdx = ( isMovingSkill ) ? FindGhostTileIndex( context, actionRangeCtrl ) : -1;

            while( ( step < 0 ) ? ( candidate >= limit ) : ( candidate <= limit ) )
            {
                actionRangeCtrl.RefreshTargetableRange( TargetingMode.DIRECTIONAL, false, isMovingSkill, tileIndex, candidate );

                if( isMovingSkill )
                {
                    int newGhostIdx = FindGhostTileIndex( context, actionRangeCtrl );

                    bool isValid = ( step < 0 )
                        ? 0 <= newGhostIdx
                        : IsGhostFartherThan( context, newGhostIdx, currentGhostIdx );

                    if( isValid )
                    {
                        currentRange = candidate;
                        RefreshAfterCurrentRangeChanged( context, currentRange, isMovingSkill, ref attackTargets, ref targetCharacter );
                        // isMovingSkillの場合、AdjustRangeForMoveによってゴーストが候補レンジより近い位置に
                        // 制限されることがある。currentRangeをゴーストの実効レンジに合わせて更新する。
                        var ghostObj = context.Owner.GhostObj;
                        if( ghostObj != null && ghostObj.gameObject.activeSelf )
                        {
                            currentRange = context.StageCtrl.CalculateTotalRange( tileIndex, ghostObj.TileIndex );
                        }
                        
                        return true;
                    }

                    candidate += step;
                }
                else
                {
                    currentRange = candidate;
                    RefreshAfterCurrentRangeChanged( context, currentRange, isMovingSkill, ref attackTargets, ref targetCharacter );
                    return true;
                }
            }

            // 有効なレンジが見つからなかった場合、元の状態を復元する
            RefreshAfterCurrentRangeChanged( context, currentRange, isMovingSkill, ref attackTargets, ref targetCharacter );
            return false;
        }

        static private void AdjustRangeForMove( TargetingRangeContext context, int tileIndex, ActionableTileData actionableTileData )
        {
            int farthestMoveableIdx      = -1;
            TileDynamicData farthestData = null;

            // ターゲット可能タイルをtileIndexから近い順にソート
            var sortedTargetableMap = actionableTileData.TargetableTileMap
                .OrderBy( pair => context.StageCtrl.CalculateTotalRange( tileIndex, pair.Key ) );

            // ターゲット可能タイルのうち、最も遠い移動可能なタイルを探す(ゴーストの表示位置)
            foreach( var targetableMap in sortedTargetableMap )
            {
                // キャラクターが存在する場合は移動不可
                if( targetableMap.Value.IsExistCharacter() )
                {
                    // actionableTileData.DeleteTargetableTile( targetableMap.Key );
                    continue;
                }

                farthestMoveableIdx = targetableMap.Key;
                farthestData        = targetableMap.Value;
            }

            // ゴースト位置より遠い敵タイルを攻撃対象から除外する
            if( 0 <= farthestMoveableIdx )
            {
                int ghostRange = context.StageCtrl.CalculateTotalRange( tileIndex, farthestMoveableIdx );
                var toRemove = actionableTileData.RefAttackTargetTileIndicies
                    .Where( idx => context.StageCtrl.CalculateTotalRange( tileIndex, idx ) > ghostRange )
                    .ToList();
                foreach( int idx in toRemove )
                {
                    actionableTileData.DeleteAttackTargetTileIndex( idx );
                }
            }

            if( farthestMoveableIdx < 0  )
            {
                actionableTileData.ClearTargetableTile();
                actionableTileData.ClearAttackTargetTileIndicies();
            }

            // ゴーストのプレビューを更新
            RefreshGhostPreview( context, farthestMoveableIdx );
        }

        static private void RefreshAfterCurrentRangeChanged( TargetingRangeContext context, int currentRange, bool isMovingSkill, ref List<CharacterKey> attackTargets, ref Character targetCharacter )
        {
            var actionRangeCtrl = context.Owner.BattleLogic.ActionRangeCtrl;
            int tileIndex       = context.Owner.BattleParams.TmpParam.CurrentTileIndex;

            actionRangeCtrl.RefreshTargetableRange( TargetingMode.DIRECTIONAL, false, isMovingSkill, tileIndex, currentRange );
            attackTargets = actionRangeCtrl.GetAttackTargetCharacterKeys();

            var prevTarget = targetCharacter;
            if( 0 < attackTargets.Count )
            {
                if( targetCharacter == null || !attackTargets.Contains( targetCharacter.GetCharacterKey() ) )
                {
                    targetCharacter = context.BtlRtnCtrl.BtlCharaCdr.GetNearestCharacter( context.Owner, attackTargets );
                }
            }
            else
            {
                targetCharacter = null;
            }

            if( prevTarget != targetCharacter )
            {
                if( targetCharacter != null )
                {
                    context.StageCtrl.ApplyTargetCursor2CharacterTile( true, targetCharacter );
                    context.BtlRtnCtrl.BtlCharaCdr.ApplyDamageExpect( context.Owner, targetCharacter );
                }
                else
                {
                    context.StageCtrl.ApplyGridCursor2CharacterTileWithFocusCamera( context.Owner );
                    context.StageCtrl.SetActiveTargetCursor( false );
                }
            }

            context.Presenter.SetActiveActionResultExpect( targetCharacter != null, ParameterWindowType.Left );
        }

        /// <summary>
        /// 指定のタイル位置にキャラクターのゴーストを配置します
        /// 指定のインデックスが無効な場合、ゴーストを表示しません
        /// </summary>
        static private void RefreshGhostPreview( TargetingRangeContext context, int tileIdx )
        {
            if( tileIdx < 0 )
            {
                context.Owner.SetGhostActive( false );
                return;
            }

            var ghostObject = context.Owner.GetGhostObject();
            var targetTile  = context.StageCtrl.GetTileStaticData( tileIdx );
            var originalRot = Quaternion.LookRotation( Vector3.forward, Vector3.up );
            var rotatedRot  = Quaternion.LookRotation( context.Owner.GetTransformHandler.GetOrderedForward(), Vector3.up );
            ghostObject.TileIndex = tileIdx;
            ghostObject.transform.SetPositionAndRotation( targetTile.CharaStandPos, rotatedRot * Quaternion.Inverse( originalRot ) );

            context.Owner.SetGhostActive( true );
        }

        /// <summary>
        /// ターゲット可能範囲内で敵が存在しないタイルのうち、スキル使用者から最も遠いタイルのインデックスを返します。
        /// 敵が存在しない有効なタイルが1つもない場合は -1 を返します。
        /// isMovingSkillがtrueの場合はキャラクターの存在有無で判定します。
        /// (AdjustRangeForMoveが攻撃対象インデックスを変更するため、attackTargetIndicesは信頼できないため)
        /// </summary>
        static private int FindGhostTileIndex( TargetingRangeContext context, ActionRangeController actionRangeCtrl )
        {
            var actionableTileData  = actionRangeCtrl.ActionableTileData;
            var targetableTileMap   = actionableTileData.TargetableTileMap;

            if( targetableTileMap.Count <= 0 || targetableTileMap.Last().Value.IsExistCharacter() )
            {
                return -1;
            }

            return targetableTileMap.Last().Key;
        }

        /// <summary>
        /// newIdxのタイルがprevIdxのタイルよりオーナーから遠い位置にあるか判定します。
        /// </summary>
        static private bool IsGhostFartherThan( TargetingRangeContext context, int newIdx, int prevIdx )
        {
            if( newIdx < 0 ) { return false; }
            if( prevIdx < 0 ) { return true; }

            Vector3 ownerPos = context.Owner.GetTransformHandler.GetPosition();
            float newDist  = Vector3.Distance( ownerPos, context.StageCtrl.GetTileStaticData( newIdx ).CharaStandPos );
            float prevDist = Vector3.Distance( ownerPos, context.StageCtrl.GetTileStaticData( prevIdx ).CharaStandPos );
            return newDist > prevDist;
        }

        static private bool IsExistOpponent( TileDynamicData tileDynamicData, CHARACTER_TAG ownerTag )
        {
            return tileDynamicData.CharaKey.IsValid() && BattleLogicBase.IsOpponentFaction[( int ) ownerTag]( tileDynamicData.CharaKey.CharacterTag );
        }
    }
}
