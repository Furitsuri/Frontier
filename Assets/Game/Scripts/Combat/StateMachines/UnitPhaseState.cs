using Frontier.Entities;
using Frontier.Stage;
using Frontier.StateMachine;
using Frontier.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Frontier.Battle
{
    public class UnitPhaseState : PhaseStateBase
    {
        [Inject] protected BattleRoutineController _btlRtnCtrl  = null;
        [Inject] protected StageController _stageCtrl           = null;
        [Inject] protected SkillActionReservationQueue _reservationQueue = null;

        protected BattleRoutinePresenter _presenter = null;

        public override void AssignPresenter( PhasePresenterBase presenter )
        {
            _presenter = presenter as BattleRoutinePresenter;
        }

        /// <summary>
        /// 死亡したキャラクターの存在を通知します
        /// </summary>
        /// <param name="characterKey">死亡したキャラクターのハッシュキー</param>
        protected void NorifyCharacterDied( in CharacterKey characterKey )
        {
            _btlRtnCtrl.BtlCharaCdr.SetDiedCharacterKey( characterKey );
            _btlRtnCtrl.BtlCharaCdr.RemoveCharacterFromList( characterKey );
        }

        /// <summary>
        /// 攻撃・スキルの対象選択ステートから退出する際の共通後始末を行います。
        /// キャンセルされて実行に至らなかった場合でも(ExitStateを通る限り)呼ばれることに注意してください。
        /// </summary>
        protected void CleanupTargetSelectionState( Character ownerChara, Character targetChara )
        {
            _stageCtrl.UnbindGridCursor();                                          // アタッカーキャラクターの設定を解除
            _stageCtrl.SetActiveTargetCursor( false );                              // ターゲットカーソルを非表示
            _stageCtrl.ApplyGridCursor2CharacterTileWithFocusCamera( ownerChara );  // オーナーのタイルにグリッドカーソルとカメラを適用

            //死亡判定を通知(相手のカウンターによって倒される可能性もあるため、攻撃者と被攻撃者の両方を判定)
            Character diedCharacter = null;
            if( ownerChara.GetStatusRef.IsDead() ) { diedCharacter = ownerChara; }
            if( targetChara != null && targetChara.GetStatusRef.IsDead() ) { diedCharacter = targetChara; }
            if( diedCharacter != null )
            {
                var key = new CharacterKey( diedCharacter.GetStatusRef.characterTag, diedCharacter.GetStatusRef.characterIndex );
                NorifyCharacterDied( key );
                ForceEndExhaustedReservations( key );
                diedCharacter.Dispose();    // 破棄
            }

            _presenter.SetActiveActionResultExpect( false, ParameterWindowType.Left );    // アクション対象指定関連のUIを非表示

            // 予測ダメージと使用スキルコスト見積もりをリセット
            if( null != ownerChara )
            {
                ownerChara.BattleParams.TmpParam.SetExpectedHpChange( 0, 0 );
            }
            if( null != targetChara )
            {
                targetChara.BattleParams.TmpParam.SetExpectedHpChange( 0, 0 );
            }

            // タイルメッシュの描画をすべてクリア
            ownerChara.BattleLogic.ActionRangeCtrl.ActionableRangeRdr.ClearTileMeshesByType( TileMapType.ATTACKABLE | TileMapType.TARGETABLE | TileMapType.QUEUED );
        }

        /// <summary>
        /// 死亡したキャラクターを攻撃対象としていた予約のうち、攻撃対象が全滅したものを強制的に行動終了させます。
        /// 予約していたスキルは実行されず、その場でゲージのみ消費して行動を終えます
        /// （攻撃対象の一部のみが死亡し、他に生存対象が残っている予約はそのまま継続されます）。
        /// </summary>
        private void ForceEndExhaustedReservations( in CharacterKey deadTargetKey )
        {
            var exhausted = _reservationQueue.RemoveDeadTargetFromAll( deadTargetKey );
            foreach( var reservation in exhausted )
            {
                var attacker = _btlRtnCtrl.BtlCharaCdr.GetPlayer( reservation.AttackerKey );
                if( attacker == null ) { continue; }

                attacker.BattleLogic.ConsumeActionGauge( reservation.ActGaugeConsumption );

                attacker.SetGhostActive( false );
                attacker.BattleLogic.ActionRangeCtrl.ClearMoveDirectionArrows();
                attacker.BattleLogic.ActionRangeCtrl.ActionableRangeRdr.ClearTileMeshesAllType();
                _stageCtrl.TileDataHdlr().ReleaseTile( reservation.GhostTileIndex );

                attacker.BattleParams.TmpParam.EndAction();
                attacker.ClearCommandHistory();
            }
        }
    }
}