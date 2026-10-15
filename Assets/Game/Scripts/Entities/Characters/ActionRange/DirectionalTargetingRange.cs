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
            int tileIndex        = context.Owner.BattleParams.TmpParam.CurrentTileIndex;
            currentRange = maxRange;    // 方向指定を受け入れたタイミングでは最大レンジに設定しておき、以降の調整で有効なレンジを探す

            context.Owner.OrderRotate( Quaternion.Euler( 0f, StageDirectionConverter.DirectionAngles[( int ) dir], 0f ) );
            actionRangeCtrl.RefreshTargetableRange( TargetingMode.DIRECTIONAL, false, isMovingSkill, tileIndex, currentRange );

            // isMovingSkillの場合、壁やステージ端でゴーストがmaxRangeより手前に制限されることがある。
            // ここでcurrentRangeを実際のゴースト距離に同期しておかないと、次回のTryAdjustRangeが
            // 「名目上のレンジ」を基準に動いてしまい、1回目の縮小操作が見た目に反映されない不具合が起きる
            if( isMovingSkill )
            {
                SnapRangeToGhostDistance( context, tileIndex, ref currentRange );
            }
        }

		static public void RefreshTargetableRange( TargetingRangeContext context, bool isFirstRefresh, bool isMovingSkill, int tileIndex, int range, ActionableTileData actionableTileData )
		{
			Vector3 basePos = context.StageCtrl.GetTileStaticData( context.Owner.BattleParams.TmpParam.CurrentTileIndex ).CharaStandPos;
			Vector3 baseForward = context.Owner.GetOrderedForward();
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
			// 攻撃対象が存在しなくても、移動可能なタイルがあれば実行可能とする(全てのタイルが移動不可の場合はゴーストを表示せず実行不可)
			if( isMovingSkill )
			{
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
        /// step方向にcurrentRangeを調整します。有効なレンジが見つかれば更新します。
        /// 有効なレンジが存在しない場合は元の状態を復元してfalseを返します。
        /// isMovingSkillの場合、候補レンジをトリミングした後(AdjustRangeForMove適用後)に
        /// 着地先(ゴースト)が存在するかどうかを有効性の条件とします。
        /// 移動を伴うスキルは攻撃対象が存在しなくても実行可能とするため、攻撃対象の有無は問いません。
        /// </summary>
        static public bool TryAdjustRange( TargetingRangeContext context, int step, ref int currentRange, int maxRange, bool isMovingSkill, ref List<CharacterKey> attackTargets, ref Character targetCharacter )
        {
            var actionRangeCtrl = context.Owner.BattleLogic.ActionRangeCtrl;
            int tileIndex       = context.Owner.BattleParams.TmpParam.CurrentTileIndex;

            int limit           = ( step < 0 ) ? 1 : maxRange;
            int candidate       = currentRange + step;
            int prevGhostTileIdx = GetActiveGhostTileIndex( context );

            while( ( step < 0 ) ? ( candidate >= limit ) : ( candidate <= limit ) )
            {
                actionRangeCtrl.RefreshTargetableRange( TargetingMode.DIRECTIONAL, false, isMovingSkill, tileIndex, candidate );

                if( isMovingSkill )
                {
                    // 着地先(ゴースト)が存在し、かつ調整前から位置が変化した場合のみ有効とする
                    // (位置が変わらないレンジを有効とすると、操作しても見た目に変化しない調整となってしまうため)
                    int ghostTileIdx = GetActiveGhostTileIndex( context );
                    bool isValid     = 0 <= ghostTileIdx && ghostTileIdx != prevGhostTileIdx;

                    if( isValid )
                    {
                        currentRange = candidate;
                        RefreshAfterCurrentRangeChanged( context, currentRange, isMovingSkill, ref attackTargets, ref targetCharacter );
                        // isMovingSkillの場合、AdjustRangeForMoveによってゴーストが候補レンジより近い位置に
                        // 制限されることがある。currentRangeをゴーストの実効レンジに合わせて更新する。
                        SnapRangeToGhostDistance( context, tileIndex, ref currentRange );

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
            int ghostTileIdx = ResolveGhostTile( tileIndex, actionableTileData, context.StageCtrl, context.Owner.GetCharacterKey(), context.GhostResolver );

            // ゴースト位置より遠いタイルをターゲット可能マップおよび攻撃対象から除外する
            // スキル固有フィルタ (context.RangeAdjustmentFilter) がある場合はそれに委譲し、
            // ない場合はデフォルト（ゴースト距離より遠いタイルをすべて除外）を適用する
            if( 0 <= ghostTileIdx )
            {
                int ghostRange = context.StageCtrl.CalculateTotalRange( tileIndex, ghostTileIdx );
                var rangeFilter = context.RangeAdjustmentFilter;
                var toRemove = actionableTileData.TargetableTileMap.Keys
                    .Where( idx =>
                    {
                        int dist = context.StageCtrl.CalculateTotalRange( tileIndex, idx );
                        if( dist <= ghostRange ) { return false; }
                        // スキル固有フィルタがあれば委譲、なければデフォルト（全除外）
                        return rangeFilter == null || !rangeFilter( idx, ghostRange, dist, actionableTileData.TargetableTileMap[idx] );
                    } )
                    .ToList();
                foreach( int idx in toRemove )
                {
                    actionableTileData.DeleteAttackTargetTileIndex( idx );
                    actionableTileData.DeleteTileMap( TileMapType.TARGETABLE, idx );
                }
            }
            else
            {
                actionableTileData.ClearTargetableTile();
                actionableTileData.ClearAttackTargetTileIndicies();
            }

            // ゴーストのプレビューを更新
            RefreshGhostPreview( context, ghostTileIdx );
        }

        /// <summary>
        /// 移動を伴うスキルについて、出発タイルから前後左右いずれかの方向に着地先(ゴースト)が存在するかを判定します。
        /// 対象選択時と同じ着地先の決定処理を、方向ごとに適用して判定します。
        /// </summary>
        /// <param name="actionableTileData">攻撃可能範囲の計算済みデータ(AttackableTileMapのみ参照し、変更しません)</param>
        static public bool HasLandingTileInAnyDirection( int dprtTileIdx, ActionableTileData actionableTileData, StageController stageCtrl, CharacterKey selfKey, TileDataHandler.GhostTileResolver resolver )
        {
            Vector3 basePos = stageCtrl.GetTileStaticData( dprtTileIdx ).CharaStandPos;
            Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };

            foreach( var dir in directions )
            {
                // 対象選択時のRefreshTargetableRangeと同様に、向きに沿ったタイルをターゲット可能タイルとして抽出する
                var dirTileData = new ActionableTileData();
                foreach( var attackableMap in actionableTileData.AttackableTileMap )
                {
                    var targetTilePos = stageCtrl.GetTileStaticData( attackableMap.Key ).CharaStandPos;
                    if( !Methods.IsMatchForward( dir, basePos, targetTilePos ) ) { continue; }

                    dirTileData.AddTargetableTile( attackableMap.Key, attackableMap.Value );
                }

                if( 0 <= ResolveGhostTile( dprtTileIdx, dirTileData, stageCtrl, selfKey, resolver ) ) { return true; }
            }

            return false;
        }

        /// <summary>
        /// ゴーストタイルを決定します。スキル固有リゾルバがあればそれを使い、なければデフォルト（最遠の空きタイル）を用います。
        /// </summary>
        static private int ResolveGhostTile( int tileIndex, ActionableTileData actionableTileData, StageController stageCtrl, CharacterKey selfKey, TileDataHandler.GhostTileResolver resolver )
        {
            return resolver != null
                ? resolver( tileIndex, actionableTileData, stageCtrl, selfKey )
                : ResolveDefaultGhostTile( tileIndex, actionableTileData, stageCtrl, selfKey );
        }

        /// <summary>
        /// デフォルトのゴーストタイル決定ロジック。ターゲット可能タイルのうち最も遠い空きタイルを返します。
        /// </summary>
        static private int ResolveDefaultGhostTile( int tileIndex, ActionableTileData actionableTileData, StageController stageCtrl, CharacterKey selfKey )
        {
            int ghostTileIdx = -1;
            var sortedTargetableMap = actionableTileData.TargetableTileMap
                .OrderBy( pair => stageCtrl.CalculateTotalRange( tileIndex, pair.Key ) );

            foreach( var targetableMap in sortedTargetableMap )
            {
                // 立てないタイル(生存キャラクターが存在する、または他キャラクターが着地予約(RESERVED)している)は候補から除外する
                if( !targetableMap.Value.IsStandableBy( selfKey ) ) { continue; }
                ghostTileIdx = targetableMap.Key;
            }

            return ghostTileIdx;
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
            var rotatedRot  = Quaternion.LookRotation( context.Owner.GetOrderedForward(), Vector3.up );
            ghostObject.TileIndex = tileIdx;
            ghostObject.transform.SetPositionAndRotation( targetTile.CharaStandPos, rotatedRot * Quaternion.Inverse( originalRot ) );

            context.Owner.SetGhostActive( true );
        }

        /// <summary>
        /// currentRangeを、実際に配置されているゴーストのタイルまでの距離に同期します。
        /// AdjustRangeForMoveは壁やステージ端でゴーストを候補レンジより手前に制限することがあるため、
        /// 名目上のレンジ値のまま次の調整を行うと「実際には変化のない縮小・拡大」が発生してしまいます。
        /// </summary>
        static private void SnapRangeToGhostDistance( TargetingRangeContext context, int tileIndex, ref int currentRange )
        {
            var ghostObj = context.Owner.GhostObj;
            if( ghostObj != null && ghostObj.gameObject.activeSelf )
            {
                currentRange = context.StageCtrl.CalculateTotalRange( tileIndex, ghostObj.TileIndex );
            }
        }

        /// <summary>
        /// 表示中のゴーストのタイルインデックスを取得します。ゴーストが表示されていない場合は-1を返します。
        /// </summary>
        static private int GetActiveGhostTileIndex( TargetingRangeContext context )
        {
            var ghostObj = context.Owner.GhostObj;
            return ( ghostObj != null && ghostObj.gameObject.activeSelf ) ? ghostObj.TileIndex : -1;
        }

        static private bool IsExistOpponent( TileDynamicData tileDynamicData, CHARACTER_TAG ownerTag )
        {
            return tileDynamicData.CharaKey.IsValid() && BattleLogicBase.IsOpponentFaction[( int ) ownerTag]( tileDynamicData.CharaKey.CharacterTag );
        }
    }
}
