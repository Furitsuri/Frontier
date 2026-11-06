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
            // MEMO : この行動でフェーズが終了する場合も、ここでオーナーへカーソルを戻す。フェーズはこのフレームのうちに終了し、
            //        次のフェーズの開始時に(カメラの注視点が実際に更新されるGridCursor.Updateより前に)カーソルが次の注視対象へ合わせ直されるため、
            //        カーソルの表示やカメラがオーナーへ戻る動きが画面に現れることはない
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
        /// 通常攻撃の予測ダメージを算出し、攻撃対象のHPゲージに予測ダメージ分の点滅を反映します。
        /// 攻撃側・防御側のスキルの使用切り替えで予測値が変わるため、切り替えの度に呼び出してください。
        /// </summary>
        protected void RefreshNormalAttackDamageExpect( Character attacker, Character target )
        {
            _btlRtnCtrl.BtlCharaCdr.ApplyDamageExpect( attacker, target );

            var expectedChange = _btlRtnCtrl.BtlCharaCdr.CalculateExpectedHpChange( attacker, target );
            _presenter.SetPredictedDamageOnCharacter( target, -expectedChange.total );
        }

        /// <summary>
        /// 指定のパラメータウィンドウにキャラクターを割り当てて表示します。
        /// </summary>
        protected void ShowParameterWindow( ParameterWindowType winType, Character chara )
        {
            var layerMaskIndex = BattleRoutinePresenter.GetLayerMaskIndexFromWinType( winType );
            _presenter.CharaParamView( winType ).AssignCharacter( chara, layerMaskIndex );
            _presenter.CharaParamView( winType ).SetActive( true );
        }

        /// <summary>
        /// 指定のパラメータウィンドウを閉じます。
        /// 表示していたキャラクターの割り当ても解除するため、死亡して破棄されるキャラクターを表示し続けることもありません。
        /// MEMO : 攻撃・スキルのシーケンス中のカメラ演出(AttackSequenceCameraHandler等)は終了時に左右のウィンドウを再表示するため、
        ///        シーケンスを伴う行動の後に閉じる場合は、必ずシーケンス完了後(各ステートのENDフェーズ等)に呼び出してください。
        /// </summary>
        protected void CloseParameterWindow( ParameterWindowType winType )
        {
            _presenter.CharaParamView( winType ).ClearCharacter();
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