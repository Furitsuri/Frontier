using System.Collections.Generic;
using Frontier.Battle;
using Frontier.Combat;
using Frontier.Stage;
using Frontier.UI;

namespace Frontier.Entities
{
    public static class NormalAttackTargetingRange
    {
        /// <summary>
        /// 通常攻撃は方向転換した際の挙動がDirectionalTargetingRangeの処理と変わらないため、そのまま呼び出しています。
        /// </summary>
        static public void AcceptDirection( TargetingRangeContext context, Direction dir, bool isWithMove, ref int currentRange, int maxRange )
        {
            DirectionalTargetingRange.AcceptDirection( context, dir, isWithMove, ref currentRange, maxRange );
        }

        static public void RefreshTargetableRange( TargetingRangeContext context, bool isFirstRefresh, bool isWithMove, int tileIndex, int currentRange, ActionableTileData actionableTileData )
        {
            foreach( var attackableMap in actionableTileData.AttackableTileMap )
            {
                if( Methods.HasAnyFlag( attackableMap.Value.Flag, TileBitFlag.ATTACKABLE_TARGET_EXIST ) )
                {
                    actionableTileData.AddAttackTargetTileIndex( attackableMap.Key );
                }
            }

            if( isFirstRefresh && 0 < actionableTileData.RefAttackTargetTileIndicies.Count )
            {
                var attackTargetKeys = context.Owner.BattleLogic.ActionRangeCtrl.GetAttackTargetCharacterKeys();
                var target = context.BtlRtnCtrl.BtlCharaCdr.GetNearestLineOfSightCharacter( context.Owner, attackTargetKeys );
                if( null == target ) { target = context.BtlRtnCtrl.BtlCharaCdr.GetNearestCharacter( context.Owner, attackTargetKeys ); }
                context.StageCtrl.ApplyTargetCursor2CharacterTile( true, target );
            }
        }

        static public void RefreshFocusTarget( TargetingRangeContext context, bool isMovingSkill, ref List<CharacterKey> attackTargetCharaKeys, ref Character targetCharacter )
        {
            attackTargetCharaKeys = context.Owner.BattleLogic.ActionRangeCtrl.GetAttackTargetCharacterKeys();

            var prevTargetCharacter = targetCharacter;
            targetCharacter         = context.BtlRtnCtrl.BtlCharaCdr.GetTargetCharacter();

            if( prevTargetCharacter == targetCharacter ) { return; }

            var layerMaskIndex = BattleRoutinePresenter.GetLayerMaskIndexFromWinType( ParameterWindowType.Right );
            context.Presenter.CharaParamView( ParameterWindowType.Right ).AssignCharacter( targetCharacter, layerMaskIndex );

            if( null != prevTargetCharacter )
            {
                prevTargetCharacter.ResetRotationOrder();
            }

            if( targetCharacter != null )
            {
                var targetTileData   = context.StageCtrl.GetTileStaticData( targetCharacter.BattleParams.TmpParam.CurrentTileIndex );
                var attackerTileData = context.StageCtrl.GetTileStaticData( context.Owner.BattleParams.TmpParam.CurrentTileIndex );
                context.Owner.RotateToPosition( targetTileData.CharaStandPos );
                targetCharacter.RotateToPosition( attackerTileData.CharaStandPos );

                targetCharacter.RefreshUseableSkillFlags( SituationType.DEFENCE, Methods.ToBit( ActionType.BUFF ) | Methods.ToBit( ActionType.SPECIAL ) );
                targetCharacter.BattleLogic.SelectUseSkills( SituationType.DEFENCE );

                context.BtlRtnCtrl.BtlCharaCdr.ApplyDamageExpect( context.Owner, targetCharacter );
            }

            context.Presenter.SetActiveActionResultExpect( targetCharacter != null, ParameterWindowType.Left );
        }

        static public bool TryAdjustRange( TargetingRangeContext context, int step, ref int currentRange, int maxRange, bool isMovingSkill, ref List<CharacterKey> attackTargets, ref Character targetCharacter )
        {
            return false;
        }

        static public int GetGhostTileIndex()
        {
            return -1;
        }
    }
}
