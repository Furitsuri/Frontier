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
        /// 攻撃・スキルの対象選択ステートから退出する際の共通後始末を行います。
        /// キャンセルされて実行に至らなかった場合でも(ExitStateを通る限り)呼ばれることに注意してください。
        /// MEMO : 死亡判定・除去はキャラクター自身が死亡アニメーション完了時に通知する形に一本化されているため
        ///        (BattleAnimationEventReceiver.DieOnAnimEvent → BattleCharacterCoordinator.NotifyCharacterDied)、
        ///        ここでは行いません。
        /// </summary>
        protected void CleanupTargetSelectionState( Character ownerChara, Character targetChara )
        {
            _stageCtrl.UnbindGridCursor();                                          // アタッカーキャラクターの設定を解除
            _stageCtrl.SetActiveTargetCursor( false );                              // ターゲットカーソルを非表示
            _stageCtrl.ApplyGridCursor2CharacterTileWithFocusCamera( ownerChara );  // オーナーのタイルにグリッドカーソルとカメラを適用

            _presenter.SetActiveActionResultExpect( false, ParameterWindowType.Left );    // アクション対象指定関連のUIを非表示
            _presenter.ClearAllPredictedDamage();                                         // HPゲージの予測ダメージ点滅表示を解除
            _btlRtnCtrl.GetBtlCameraCtrl.SetTargetSelectZoomActive( false );               // 対象選択中のカメラズームを解除

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
        /// 攻撃・スキルの対象選択ステートから、実行確定(攻撃シーケンスへの移行、予約キューへの登録等)を行う際の共通後始末を行います。
        /// CleanupTargetSelectionStateと異なり、グリッドカーソルの解除やオーナータイルへのカメラ復帰までは行いません
        /// (実行確定直後はまだ攻撃シーケンス等が続くため、その後始末はExitState側のCleanupTargetSelectionStateに委ねます)。
        /// </summary>
        protected void FinalizeTargetSelection( ParameterWindowType fromWinType )
        {
            _stageCtrl.SetActiveGridCursor( false );                                   // 選択グリッドを一時非表示
            _stageCtrl.SetActiveTargetCursor( false );                                 // ターゲットカーソルを一時非表示
            _presenter.SetActiveActionResultExpect( false, fromWinType );              // アクション対象指定関連のUIを非表示
            _presenter.ClearAllPredictedDamage();                                      // HPゲージの予測ダメージ点滅表示を解除
            _btlRtnCtrl.GetBtlCameraCtrl.SetTargetSelectZoomActive( false );            // 対象選択中のカメラズームを解除
        }
    }
}