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

            /*
             _stageCtrl.OperateGridCursorController( dir );

            var actionRangeCtrl     = _plOwner.BattleLogic.ActionRangeCtrl;
            var targetingRangeIndex = _stageCtrl.GetCurrentGridIndex();

            // ターゲット指定中のグリッドが攻撃可能なタイルを指していない場合は、ターゲットキャラクターを再設定せずに処理を終了する
            if( !_plOwner.BattleLogic.ActionRangeCtrl.ActionableTileData.AttackableTileMap.Keys.Contains( targetingRangeIndex ) )
            {
                return;
            }

            actionRangeCtrl.RefreshTargetableRange( _targetingMode, _isMovingSkill, targetingRangeIndex, _currentRange );
            _attackTargetCharaKeys = actionRangeCtrl.GetAttackTargetCharacterKeys();

            // 攻撃可能なグリッド内に敵がおり、尚且つ現在のターゲットキャラクターが存在しない場合は、ターゲットキャラクターを設定する
            if( 0 < _attackTargetCharaKeys.Count )
            {
                if( null == _targetCharacter )
                {
                    _targetCharacter = _btlRtnCtrl.BtlCharaCdr.GetNearestCharacter( _plOwner, _attackTargetCharaKeys );
                    Debug.Assert( _targetCharacter != null, "攻撃可能なグリッドが存在する場合、必ずターゲットキャラクターが存在するはずですが、nullが返されました。" );
                    _stageCtrl.MoveGridCursorToAttackableTile( _targetCharacter );
                }
                else
                {
                    // 現在のターゲットキャラクターが攻撃可能なグリッド内にいる場合は、ターゲットキャラクターを再設定しない
                    if( !_attackTargetCharaKeys.Contains( _targetCharacter.GetCharacterKey() ) )
                    {
                        _targetCharacter = _btlRtnCtrl.BtlCharaCdr.GetNearestCharacter( _plOwner, _attackTargetCharaKeys );
                        Debug.Assert( _targetCharacter != null, "攻撃可能なグリッドが存在する場合、必ずターゲットキャラクターが存在するはずですが、nullが返されました。" );
                        _stageCtrl.MoveGridCursorToAttackableTile( _targetCharacter );
                    }
                }
            }
            else
            {
                _targetCharacter = null;
            }

            _presenter.SetActiveActionResultExpect( _targetCharacter != null, ParameterWindowType.Left );
             */
        }

        static public void RefreshTargetableRange( TargetingRangeContext context, bool isWithMove, int tileIndex, int currentRange, ActionableTileData actionableTileData )
        {
            context.StageCtrl.TileDataHdlr().BeginExpandTargetableTilesWithPartOfRange( tileIndex, currentRange, context.Owner.GetCharacterTag(), actionableTileData );
        }

		static public void RefreshFocusTarget( TargetingRangeContext context, ref List<CharacterKey> attackTargetCharaKeys, ref Character targetCharacter, bool isMovingSkill, Action<ActionRangeController> refreshGhostCallback )
		{
		}

		static public bool TryAdjustRange( TargetingRangeContext context, int step, ref int currentRange, int maxRange, bool isMovingSkill, ref List<CharacterKey> attackTargets, ref Character targetCharacter )
        {
            return false;
        }
    }
}
