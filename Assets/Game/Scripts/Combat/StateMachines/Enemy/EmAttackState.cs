using Frontier.Combat;
using Frontier.Entities;
using Frontier.Sequences;
using Frontier.Stage;
using Frontier.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.Battle
{
    public class EmAttackState : UnitPhaseState
    {
        private enum EmAttackPhase
        {
            EM_ATTACK_CONFIRM = 0,
            EM_ATTACK_EXECUTE,
            EM_ATTACK_END,
        }

        private enum TransitTag
        {
            DEFENCE_SKILL_SELECT = 0,
        }

        [Inject] private SequenceFacade _sequenceFcd = null;

        private EmAttackPhase _phase;
        private Enemy _attackCharacter = null;
        private Character _targetCharacter = null;
        private bool _isWaitingForDefenceSkillSelect = false;

        public override void Init( object context )
        {
            base.Init( context);

            _attackCharacter = context as Enemy;    // 手番の敵はEmSelectStateから渡される
            Debug.Assert( _attackCharacter != null );
            _isWaitingForDefenceSkillSelect = false;

            // 現在選択中のキャラクター情報を取得して攻撃範囲を表示
            _attackCharacter.BattleLogic.ActionRangeCtrl.SetupAttackableRangeData( _attackCharacter.BattleParams.TmpParam.CurrentTileIndex );
            _attackCharacter.BattleLogic.ActionRangeCtrl.DrawAttackableRange();

			// アタッカーキャラクターの設定
			_stageCtrl.BindGridCursor( GridCursorState.ATTACK, _attackCharacter );

            // 対象選択中はカメラを少しだけ近づける(対象の有無によらず、この場面に入った時点でON)
            _btlRtnCtrl.GetBtlCameraCtrl.SetTargetSelectZoomActive( true );

            List<int> tileIndicies = new List<int>();

            foreach( var tileDynamicData in _attackCharacter.BattleLogic.ActionRangeCtrl.ActionableTileData.AttackableTileMap )
            {
                if( Methods.HasAllFlags( tileDynamicData.Value.Flag, TileBitFlag.ATTACKABLE_TARGET_EXIST ) )
                {
                    tileIndicies.Add( tileDynamicData.Key );
                }
            }

            if( 0 < tileIndicies.Count )
            {
                _stageCtrl.ApplyTargetCursor2CharacterTile( true, _attackCharacter.BattleLogic.GetAi().GetTargetCharacter() );
                _presenter.SetActiveActionResultExpect( true, ParameterWindowType.Right );
            }

            _targetCharacter = _attackCharacter.BattleLogic.GetAi().GetTargetCharacter();
            _stageCtrl.ApplyGridCursor2CharacterTile( _attackCharacter );

            // 攻撃者の向きを設定
            var targetTileData = _stageCtrl.GetTileStaticData( _targetCharacter.BattleParams.TmpParam.CurrentTileIndex );
            _attackCharacter.RotateToPosition( targetTileData.CharaStandPos );
            var attackerTileData = _stageCtrl.GetTileStaticData( _attackCharacter.BattleParams.TmpParam.CurrentTileIndex );
            _targetCharacter.RotateToPosition( attackerTileData.CharaStandPos );

            // 攻撃対象をパラメータビュー(左)に表示する
            // (攻撃する敵自身(右)の表示と、行動終了後に左右を閉じる処理はEmSelectStateが行う)
            ShowParameterWindow( ParameterWindowType.Left, _targetCharacter );

            // 防御側のスキルの使用可否を、防御シチュエーションで更新する
            // (プレイヤーフェーズ中の攻撃シチュエーションでの判定結果が残ったままだと、防御スキルが使用不可のままになるため)
            EmDefenceSkillSelectState.RefreshDefenceUseableSkillFlags( _targetCharacter );

            // 使用スキルを選択する(プレイヤー以外はAIが選択。プレイヤーは防御スキル選択ステートで選択する)
            _attackCharacter.BattleLogic.SelectUseSkills( SituationType.ATTACK );
            _targetCharacter.BattleLogic.SelectUseSkills( SituationType.DEFENCE );

            // 予測ダメージを適応し、HPゲージに予測ダメージ分の点滅を反映する
            RefreshNormalAttackDamageExpect( _attackCharacter, _targetCharacter );

            _phase = EmAttackPhase.EM_ATTACK_CONFIRM;
        }

        public override bool Update()
        {
            // 攻撃可能状態でなければ何もしない
            if( _stageCtrl.GetGridCursorState() != GridCursorState.ATTACK )
            {
                return false;
            }

            switch( _phase )
            {
                case EmAttackPhase.EM_ATTACK_CONFIRM:
                    if( _isWaitingForDefenceSkillSelect ) { break; }

                    // Initで発行した、攻撃者・攻撃対象が互いに向き合う向き直りが終わるまで待つ
                    // (攻撃シーケンスは開始時点の向きを攻撃前の向きとして記録するため、
                    //  向き直りの途中で開始すると、攻撃後に移動直後の向きへ戻ってしまう)
                    if( _attackCharacter.IsRotationOrdered() || _targetCharacter.IsRotationOrdered() ) { break; }

                    // 攻撃対象のプレイヤーが防御時に使用可能なスキルを持つ場合は、防御スキルの選択へ遷移する
                    // (装備していない、またはアクションゲージが不足している場合は確認を挟まずにそのまま攻撃を実行する)
                    if( _targetCharacter is Player defender && EmDefenceSkillSelectState.HasUseableDefenceSkill( defender ) )
                    {
                        _isWaitingForDefenceSkillSelect = true;
                        SetSendTransitionContext( new EmDefenceSkillSelectState.TransitContext { Attacker = _attackCharacter, Defender = defender } );
                        TransitState( ( int ) TransitTag.DEFENCE_SKILL_SELECT );
                    }
                    else
                    {
                        ExecuteAttack();
                    }

                    break;
                case EmAttackPhase.EM_ATTACK_EXECUTE:
                    {
                        _phase = EmAttackPhase.EM_ATTACK_END;
                    }
                    break;
                case EmAttackPhase.EM_ATTACK_END:
                    // 攻撃したキャラクターの攻撃コマンドを選択不可にする
                    _attackCharacter.BattleParams.TmpParam.SetEndCommandStatus( COMMAND_TAG.ATTACK, true );

                    Back();

                    return true;
            }

            return false;
        }

        public override object ExitState()
        {
            // MEMO : 死亡判定・除去はキャラクター自身が死亡アニメーション完了時に通知する形に一本化されているため
            //        (BattleAnimationEventReceiver.DieOnAnimEvent → BattleCharacterCoordinator.NotifyCharacterDied)、
            //        ここでは行いません。

            // 対象選択の共通後始末
            // (パラメータウィンドウは攻撃シーケンス中も表示し続けるため非表示にせず、グリッドカーソルも下記の理由でオーナーへ戻さない)
            CleanupTargetSelectionState( _attackCharacter, _targetCharacter, ParameterWindowType.Right, hideTargetWindow: false, focusOwner: false );
            // 他キャラクターの描画分も含め、タイルメッシュの描画をすべてクリア
            _btlRtnCtrl.BtlCharaCdr.ClearAllTileMeshesAndGhosts();
            // 選択グリッドを表示
            // ※この攻撃の直後にプレイヤーフェーズに移行した場合、一瞬の間、選択グリッドが表示され、
            //   その後プレイヤーに選択グリッドが移るという状況になります。
            //   その挙動が少しバグのように見えてしまうので、消去したままにすることにし、
            //   次のキャラクターが行動開始する際に表示するようにします。
            // Stage.StageController.Instance.SetActiveGridCursor(true);

            return base.ExitState();
        }

        protected override void OnActivated()
        {
            base.OnActivated();

            // 防御スキルの選択から戻った場合は攻撃を実行する
            if( _isWaitingForDefenceSkillSelect )
            {
                _isWaitingForDefenceSkillSelect = false;
                ExecuteAttack();
            }
        }

        /// <summary>
        /// 攻撃を実際に実行します
        /// </summary>
        private void ExecuteAttack()
        {
            // キャラクターのアクションゲージを消費
            _attackCharacter.BattleLogic.ConsumeActionGauge();
            // 防御側は防御スキルの使用分を消費し、使用ONのスキルを使用済みへ移す
            // (攻撃シーケンス中のガードモーション・パリィ等の判定は使用済みのスキルを参照するため)
            _targetCharacter.BattleLogic.ConsumeActionGaugeForSkill();

            FinalizeTargetSelection( ParameterWindowType.Right, false );          // カメラズームの解除は攻撃シーケンス側で行う
            _btlRtnCtrl.BtlCharaCdr.ClearAllTileMeshesAndGhosts();               // タイルメッシュの描画をすべてクリア

            // 防御側が自己バフスキル(GUARD等)を使用している場合はバフシーケンスを登録する(必ず攻撃シーケンスより先に登録する)
            _targetCharacter.BattleLogic.RegistSelfBuffSequences();

            _sequenceFcd.RegistAttack( _attackCharacter, _targetCharacter );    // 攻撃シーケンス処理に移行

            _phase = EmAttackPhase.EM_ATTACK_EXECUTE;
        }
    }
}