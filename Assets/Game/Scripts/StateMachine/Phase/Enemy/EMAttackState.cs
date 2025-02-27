using Frontier.Stage;
using Frontier.Entities;
using UnityEngine;

namespace Frontier
{
    public class EMAttackState : PhaseStateBase
    {
        private enum EMAttackPhase
        {
            EM_ATTACK_CONFIRM = 0,
            EM_ATTACK_EXECUTE,
            EM_ATTACK_END,
        }

        private EMAttackPhase _phase;
        private int _curentGridIndex                    = -1;
        private Enemy _attackCharacter                  = null;
        private Character _targetCharacter              = null;
        private CharacterAttackSequence _attackSequence = null;

        override public void Init()
        {
            base.Init();

            _attackSequence = _hierarchyBld.InstantiateWithDiContainer<CharacterAttackSequence>();
            _curentGridIndex = _stageCtrl.GetCurrentGridIndex();
            _attackCharacter = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter() as Enemy;
            Debug.Assert(_attackCharacter != null);

            // 現在選択中のキャラクター情報を取得して攻撃範囲を表示
            var param = _attackCharacter.param;
            _stageCtrl.RegistAttackAbleInfo(_curentGridIndex, param.attackRange, param.characterTag);
            _stageCtrl.DrawAttackableGrids(_curentGridIndex);

            // 攻撃可能なグリッド内に敵がいた場合に標的グリッドを合わせる
            if (_stageCtrl.RegistAttackTargetGridIndexs(Character.CHARACTER_TAG.PLAYER, _attackCharacter.GetAi().GetTargetCharacter()))
            {
                // アタッカーキャラクターの設定
                _stageCtrl.BindGridCursorState(GridCursor.State.ATTACK, _attackCharacter);
                // アタックカーソルUI表示
                _uiSystem.BattleUi.ToggleAttackCursorE2P(true);
            }

            _targetCharacter = _attackCharacter.GetAi().GetTargetCharacter();
            _stageCtrl.ApplyCurrentGrid2CharacterGrid(_attackCharacter);

            // 攻撃者の向きを設定
            var targetGridInfo = _stageCtrl.GetGridInfo(_targetCharacter.GetCurrentGridIndex());
            _attackCharacter.RotateToPosition(targetGridInfo.charaStandPos);
            var attackerGridInfo = _stageCtrl.GetGridInfo(_attackCharacter.GetCurrentGridIndex());
            _targetCharacter.RotateToPosition(attackerGridInfo.charaStandPos);

            // 攻撃シーケンスを初期化
            _attackSequence.Init();

            _phase = EMAttackPhase.EM_ATTACK_CONFIRM;
        }

        override public bool Update()
        {
            // 攻撃可能状態でなければ何もしない
            if (_stageCtrl.GetGridCursorState() != GridCursor.State.ATTACK)
            {
                return false;
            }

            switch (_phase)
            {
                case EMAttackPhase.EM_ATTACK_CONFIRM:
                    // 使用スキルを選択する
                    _attackCharacter.SelectUseSkills(SkillsData.SituationType.ATTACK);
                    _targetCharacter.SelectUseSkills(SkillsData.SituationType.DEFENCE);

                    // 予測ダメージを適応する
                    _btlRtnCtrl.BtlCharaCdr.ApplyDamageExpect(_attackCharacter, _targetCharacter);

                    // ダメージ予測表示UIを表示
                    _uiSystem.BattleUi.ToggleBattleExpect(true);

                    if (Input.GetKeyUp(KeyCode.Space))
                    {
                        // キャラクターのアクションゲージを消費
                        _attackCharacter.ConsumeActionGauge();
                        _targetCharacter.ConsumeActionGauge();

                        // 選択グリッドを一時非表示
                        _stageCtrl.SetGridCursorActive(false);

                        // アタックカーソルUI非表示
                        _uiSystem.BattleUi.ToggleAttackCursorE2P(false);

                        // ダメージ予測表示UIを非表示
                        _uiSystem.BattleUi.ToggleBattleExpect(false);

                        // グリッド状態の描画をクリア
                        _stageCtrl.ClearGridMeshDraw();

                        // 攻撃シーケンスの開始
                        _attackSequence.StartSequence(_attackCharacter, _targetCharacter);

                        _phase = EMAttackPhase.EM_ATTACK_EXECUTE;
                    }
                    break;
                case EMAttackPhase.EM_ATTACK_EXECUTE:
                    if (_attackSequence.Update())
                    {
                        _phase = EMAttackPhase.EM_ATTACK_END;
                    }
                    break;
                case EMAttackPhase.EM_ATTACK_END:
                    // 攻撃したキャラクターの攻撃コマンドを選択不可にする
                    _attackCharacter.SetEndCommandStatus( Character.Command.COMMAND_TAG.ATTACK, true );
                    // コマンド選択に戻る
                    Back();

                    return true;
            }

            return false;
        }

        override public void Exit()
        {
            //死亡判定を通知(相手のカウンターによって倒される可能性もあるため、攻撃者と被攻撃者の両方を判定)
            Character diedCharacter = _attackSequence.GetDiedCharacter();
            if (diedCharacter != null)
            {
                var key = new CharacterHashtable.Key(diedCharacter.param.characterTag, diedCharacter.param.characterIndex);
                NoticeCharacterDied(key);
                // 破棄
                diedCharacter.Remove();
            }

            // アタッカーキャラクターの設定を解除
            _stageCtrl.ClearGridCursroBind();
            // 予測ダメージをリセット
            _attackCharacter.SetExpectedHpChange(0, 0);
            _targetCharacter.SetExpectedHpChange(0, 0);

            // アタックカーソルUI非表示
            _uiSystem.BattleUi.ToggleAttackCursorP2E(false);
            // ダメージ予測表示UIを非表示
            _uiSystem.BattleUi.ToggleBattleExpect(false);
            // 使用スキルの点滅を非表示
            for (int i = 0; i < Constants.EQUIPABLE_SKILL_MAX_NUM; ++i)
            {
                _uiSystem.BattleUi.GetPlayerParamSkillBox(i).SetFlickEnabled(false);
                _uiSystem.BattleUi.GetPlayerParamSkillBox(i).SetUseable(true);
                _uiSystem.BattleUi.GetEnemyParamSkillBox(i).SetFlickEnabled(false);
                _uiSystem.BattleUi.GetEnemyParamSkillBox(i).SetUseable(true);
            }
            // 使用スキルコスト見積もりをリセット
            _attackCharacter.param.ResetConsumptionActionGauge();
            _attackCharacter.skillModifiedParam.Reset();
            _targetCharacter.param.ResetConsumptionActionGauge();
            _targetCharacter.skillModifiedParam.Reset();
            // グリッド状態の描画をクリア
            _stageCtrl.UpdateGridInfo();
            _stageCtrl.ClearGridMeshDraw();
            // 選択グリッドを表示
            // ※この攻撃の直後にプレイヤーフェーズに移行した場合、一瞬の間、選択グリッドが表示され、
            //   その後プレイヤーに選択グリッドが移るという状況になります。
            //   その挙動が少しバグのように見えてしまうので、消去したままにすることにし、
            //   次のキャラクターが行動開始する際に表示するようにします。
            // Stage.StageController.Instance.SetGridCursorActive(true);

            base.Exit();
        }
    }
}