using Frontier.Combat;
using Frontier.Entities;
using Frontier.Stage;

namespace Frontier.Battle
{
    /// <summary>
    /// 予約されたスキルアクションを実行する直前に必要な、攻撃者への復元処理をまとめたヘルパーです。
    /// PlSkillActionToTargetState(連携実行)・PlSelectReservedActionState(即時実行)・
    /// PlConfirmReservedActionsState(ターン終了時一括実行)の3箇所で共通して使用します。
    /// </summary>
    public static class ReservedSkillActionApplier
    {
        /// <summary>
        /// 予約データを攻撃者に適用します(ゴースト再構築、スキル切替・ゲージ消費量の復元、
        /// キュー状態解除、ゲージ消費、自己バフ登録)。対象キャラクターを解決して返します。
        /// </summary>
        public static Character Apply( SkillActionReservationData data, Character attacker, StageController stageCtrl, BattleCharacterCoordinator btlCharaCdr )
        {
            // ゴーストを使用するスキル（ダッシュ斬りなど）のためにゴーストを再構築する
            if( data.GhostTileIndex >= 0 )
            {
                var ghost = attacker.GetGhostObject();
                ghost.TileIndex          = data.GhostTileIndex;
                ghost.transform.position = stageCtrl.GetTileStaticData( data.GhostTileIndex ).CharaStandPos;
            }

            for( int i = 0; i < data.AttackerSkillsToggledON.Length; ++i )
            {
                attacker.BattleParams.TmpParam.IsSkillsToggledON[i] = data.AttackerSkillsToggledON[i];
            }
            attacker.BattleParams.TmpParam.ActGaugeConsumption = data.ActGaugeConsumption;
            attacker.BattleParams.TmpParam.IsSkillQueued        = false;

            attacker.BattleLogic.ActionRangeCtrl.ActionableRangeRdr.ClearTileMeshesByType( TileMapType.QUEUED );
            attacker.BattleLogic.ConsumeActionGaugeForSkill();

            var target = data.FocusedTargetCharaKey.IsValid()
                ? btlCharaCdr.GetCharacter( data.FocusedTargetCharaKey )
                : null;
            if( target != null ) { target.BattleLogic.ConsumeActionGauge(); }

            if( attacker.BattleLogic.RegistSelfBuffSequences() )
            {
                attacker.RefreshUseableSkillFlags( SituationType.ATTACK, Methods.ToBit( ActionType.BUFF ) );
            }

            return target;
        }
    }
}
