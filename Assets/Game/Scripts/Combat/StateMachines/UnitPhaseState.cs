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
        /// <param name="fromWinType">行動側キャラクターを表示しているパラメータウィンドウ(プレイヤーはLeft、敵はRight)</param>
        /// <param name="hideTargetWindow">攻撃対象側のパラメータウィンドウを非表示にするか</param>
        /// <param name="focusOwner">
        /// オーナーのタイルにグリッドカーソルとカメラを戻すか。
        /// 敵の行動後は直後にプレイヤーフェーズへ移る場合があり、選択グリッドが一瞬表示されてバグのように見えるため false を指定します。
        /// </param>
        protected void CleanupTargetSelectionState( Character ownerChara, Character targetChara,
            ParameterWindowType fromWinType = ParameterWindowType.Left, bool hideTargetWindow = true, bool focusOwner = true )
        {
            UnbindGridCursorKeepingParameterView();                                 // アタッカーキャラクターの設定を解除
            _stageCtrl.SetActiveTargetCursor( false );                              // ターゲットカーソルを非表示
            if( focusOwner )
            {
                _stageCtrl.ApplyGridCursor2CharacterTileWithFocusCamera( ownerChara );  // オーナーのタイルにグリッドカーソルとカメラを適用
            }

            // アクション対象指定関連のUIを非表示
            if( hideTargetWindow )
            {
                _presenter.SetActiveActionResultExpect( false, fromWinType );
            }
            else
            {
                _presenter.SetActiveActionDirectionAndResultExpect( false, fromWinType );
            }
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
        /// グリッドカーソルとキャラクターの紐づけを解除します。
        /// GridCursor.Unbindは紐づけていたキャラクターのレイヤーを既定に戻すため、そのままでは
        /// パラメータウィンドウのカメラに映らなくなります。表示中のキャラクターは各ウィンドウのレイヤーへ再設定します。
        /// </summary>
        protected void UnbindGridCursorKeepingParameterView()
        {
            _stageCtrl.UnbindGridCursor();

            _presenter.CharaParamView( ParameterWindowType.Left ).RestoreCharacterLayer();
            _presenter.CharaParamView( ParameterWindowType.Right ).RestoreCharacterLayer();
        }

        /// <summary>
        /// 攻撃・スキルの対象選択ステートから、実行確定(攻撃シーケンスへの移行、予約キューへの登録等)を行う際の共通後始末を行います。
        /// CleanupTargetSelectionStateと異なり、グリッドカーソルの解除やオーナータイルへのカメラ復帰までは行いません
        /// (実行確定直後はまだ攻撃シーケンス等が続くため、その後始末はExitState側のCleanupTargetSelectionStateに委ねます)。
        /// </summary>
        /// <param name="releaseCameraZoom">
        /// 対象選択中のカメラズームをここで解除するか。
        /// 通常攻撃(CharacterAttackSequenceBase の派生)は解除のタイミングをシーケンス側で決めるため false を指定してください。
        /// </param>
        protected void FinalizeTargetSelection( ParameterWindowType fromWinType, bool releaseCameraZoom = true )
        {
            _stageCtrl.SetActiveGridCursor( false );                                   // 選択グリッドを一時非表示
            _stageCtrl.SetActiveTargetCursor( false );                                 // ターゲットカーソルを一時非表示
            _presenter.SetActiveActionDirectionAndResultExpect( false, fromWinType );  // アクション方向の矢印・ダメージ予測表示を非表示(実行中も対象のパラメータウィンドウは表示し続ける)
            _presenter.ClearAllPredictedDamage();                                      // HPゲージの予測ダメージ点滅表示を解除

            if( releaseCameraZoom )
            {
                _btlRtnCtrl.GetBtlCameraCtrl.SetTargetSelectZoomActive( false );        // 対象選択中のカメラズームを解除
            }
        }
    }
}