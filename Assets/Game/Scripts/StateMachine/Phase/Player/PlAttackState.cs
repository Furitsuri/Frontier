using Frontier.Stage;
using Frontier.Entities;
using System.Collections.Generic;
using UnityEngine;
using static Constants;

namespace Frontier
{
    public class PlAttackState : PlPhaseStateBase
    {
        private enum PLAttackPhase
        {
            PL_ATTACK_SELECT_GRID = 0,
            PL_ATTACK_EXECUTE,
            PL_ATTACK_END,
        }

        private PLAttackPhase _phase = PLAttackPhase.PL_ATTACK_SELECT_GRID;
        private int _curentGridIndex = -1;
        private Character _attackCharacter = null;
        private Character _targetCharacter = null;
        private CharacterAttackSequence _attackSequence = null;

        /// <summary>
        /// 初期化します
        /// </summary>
        override public void Init()
        {
            base.Init();

            // 入力ガイドを登録
            _inputFcd.RegisterInputCodes(
               (GuideIcon.ALL_CURSOR,   "TargetSelect", CanAcceptDirection, new AcceptDirectionInput(AcceptDirection), DIRECTION_INPUT_INTERVAL),
               (GuideIcon.CANCEL,       "TURN END",     CanAcceptDefault, new AcceptBooleanInput(AcceptCancel), 0.0f)
            );

            _attackSequence     = _hierarchyBld.InstantiateWithDiContainer<CharacterAttackSequence>();
            _phase              = PLAttackPhase.PL_ATTACK_SELECT_GRID;
            _curentGridIndex    = _stageCtrl.GetCurrentGridIndex();
            _targetCharacter    = null;

            // 現在選択中のキャラクター情報を取得して攻撃範囲を表示
            _attackCharacter = _btlRtnCtrl.BtlCharaCdr.GetCharacterFromHashtable(_btlRtnCtrl.SelectCharacterInfo);

            if (_attackCharacter == null)
            {
                Debug.Assert(false, "SelectPlayer Irregular.");
                return;
            }
            var param = _attackCharacter.param;
            _stageCtrl.RegistAttackAbleInfo(_curentGridIndex, param.attackRange, param.characterTag);
            _stageCtrl.DrawAttackableGrids(_curentGridIndex);

            // 攻撃可能なグリッド内に敵がいた場合に標的グリッドを合わせる
            if (_stageCtrl.RegistAttackTargetGridIndexs(Character.CHARACTER_TAG.ENEMY))
            {
                // アタッカーキャラクターの設定
                _stageCtrl.BindGridCursorState(GridCursor.State.ATTACK, _attackCharacter);

                // アタックカーソルUI表示
                _uiSystem.BattleUi.ToggleAttackCursorP2E(true);
            }

            // 攻撃シーケンスを初期化
            _attackSequence.Init();
        }

        override public bool Update()
        {
            if (base.Update())
            {
                return true;
            }

            // 攻撃可能状態でなければ何もしない
            if (_stageCtrl.GetGridCursorState() != Stage.GridCursor.State.ATTACK)
            {
                return false;
            }

            switch (_phase)
            {
                case PLAttackPhase.PL_ATTACK_SELECT_GRID:
                    // グリッド上のキャラクターを取得
                    var prevTargetCharacter = _targetCharacter;
                    _targetCharacter = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();

                    // 選択キャラクターが更新された場合は向きを更新
                    if( prevTargetCharacter != _targetCharacter )
                    {
                        var targetGridInfo = _stageCtrl.GetGridInfo(_targetCharacter.GetCurrentGridIndex());
                        _attackCharacter.RotateToPosition(targetGridInfo.charaStandPos );
                        var attackerGridInfo = _stageCtrl.GetGridInfo(_attackCharacter.GetCurrentGridIndex());
                        _targetCharacter.RotateToPosition(attackerGridInfo.charaStandPos);
                    }

                    // ダメージ予測表示UIを表示
                    _uiSystem.BattleUi.ToggleBattleExpect(true);

                    // 使用スキルを選択する
                    _attackCharacter.SelectUseSkills(SkillsData.SituationType.ATTACK);
                    _targetCharacter.SelectUseSkills(SkillsData.SituationType.DEFENCE);

                    // 予測ダメージを適応する
                    _btlRtnCtrl.BtlCharaCdr.ApplyDamageExpect(_attackCharacter, _targetCharacter);

                    if (_inputFcd.GetInputConfirm())
                    {
                        // 選択したキャラクターが敵である場合は攻撃開始
                        if (_targetCharacter != null && _targetCharacter.param.characterTag == Character.CHARACTER_TAG.ENEMY)
                        {
                            // キャラクターのアクションゲージを消費
                            _attackCharacter.ConsumeActionGauge();
                            _targetCharacter.ConsumeActionGauge();

                            // 選択グリッドを一時非表示
                            _stageCtrl.SetGridCursorActive(false);
                            
                            // アタックカーソルUI非表示
                            _uiSystem.BattleUi.ToggleAttackCursorP2E(false);

                            // ダメージ予測表示UIを非表示
                            _uiSystem.BattleUi.ToggleBattleExpect(false);

                            // グリッド状態の描画をクリア
                            _stageCtrl.ClearGridMeshDraw();

                            // 攻撃シーケンスの開始
                            _attackSequence.StartSequence(_attackCharacter, _targetCharacter);

                            _phase = PLAttackPhase.PL_ATTACK_EXECUTE;
                        }
                    }
                    break;
                case PLAttackPhase.PL_ATTACK_EXECUTE:
                    if (_attackSequence.Update())
                    {
                        _phase = PLAttackPhase.PL_ATTACK_END;
                    }

                    break;
                case PLAttackPhase.PL_ATTACK_END:
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
            _stageCtrl.SetGridCursorActive(true);

            base.Exit();
        }

        /// <summary>
        /// 方向入力の受付可否を判定します
        /// </summary>
        /// <returns>方向入力の受付可否</returns>
        override protected bool CanAcceptDirection()
        {
            if (!CanAcceptDefault()) return false;

            // 攻撃対象選択フェーズでない場合は終了
            if (PLAttackPhase.PL_ATTACK_SELECT_GRID == _phase) return true;

            return false;
        }

        /// <summary>
        /// 攻撃対象選択の入力を検知します
        /// </summary>
        /// <returns>入力の有無</returns>
        override protected bool AcceptDirection(Constants.Direction dir)
        {
            if (_stageCtrl.OperateTargetSelect(dir))
            {
                return true;
            }

            return false;
        }
    }
}