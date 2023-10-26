using Frontier.Stage;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Frontier.SkillsData;

namespace Frontier
{
    public class PLAttackState : PhaseStateBase
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
        private CharacterAttackSequence _attackSequence = new CharacterAttackSequence();

        override public void Init()
        {
            var btlUIInstance = BattleUISystem.Instance;

            base.Init();

            _phase = PLAttackPhase.PL_ATTACK_SELECT_GRID;
            _curentGridIndex = _stageCtrl.GetCurrentGridIndex();

            // 現在選択中のキャラクター情報を取得して攻撃範囲を表示
            _attackCharacter = _btlMgr.GetCharacterFromHashtable(_btlMgr.SelectCharacterInfo);
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
                btlUIInstance.ToggleAttackCursorP2E(true);
                // 攻撃者の向きを更新
                GridInfo info;
                _stageCtrl.FetchCurrentGridInfo(out info);
                _attackCharacter.OrderRotateToPosition(info.charaStandPos);
            }
        }

        public override bool Update()
        {
            var btlUIInstance = BattleUISystem.Instance;

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
                    // グリッドの操作
                    _stageCtrl.OperateGridCursor();
                    // グリッド上のキャラクターを取得
                    var prevTargetCharacter = _targetCharacter;
                    _targetCharacter = _btlMgr.GetSelectCharacter();
                    // 選択キャラクターが更新された場合は向きを更新
                    if( prevTargetCharacter != _targetCharacter )
                    {
                        var info = _stageCtrl.GetGridInfo(_targetCharacter.tmpParam.gridIndex);
                        _attackCharacter.OrderRotateToPosition( info.charaStandPos );
                    }
                    // ダメージ予測表示UIを表示
                    btlUIInstance.ToggleBattleExpect(true);
                    // 使用スキルを選択する
                    _attackCharacter.SelectUseSkills(SituationType.ATTACK);
                    _targetCharacter.SelectUseSkills(SituationType.DEFENCE);
                    // 予測ダメージを適応する
                    _btlMgr.ApplyDamageExpect(_attackCharacter, _targetCharacter);

                    if (Input.GetKeyUp(KeyCode.Space))
                    {
                        // 選択したキャラクターが敵である場合は攻撃開始
                        if (_targetCharacter != null && _targetCharacter.param.characterTag == Character.CHARACTER_TAG.ENEMY)
                        {
                            // キャラクターのアクションゲージを消費
                            _attackCharacter.ConsumeActionGauge();
                            _targetCharacter.ConsumeActionGauge();
                            // 選択グリッドを一時非表示
                            _stageCtrl.SetGridCursorActive(false);
                            // 攻撃シーケンスを初期化
                            _attackSequence.Init(_attackCharacter, _targetCharacter);
                            // アタックカーソルUI非表示
                            btlUIInstance.ToggleAttackCursorP2E(false);
                            // ダメージ予測表示UIを非表示
                            btlUIInstance.ToggleBattleExpect(false);
                            // グリッド状態の描画をクリア
                            _stageCtrl.ClearGridMeshDraw();

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
                    _attackCharacter.tmpParam.isEndCommand[(int)Character.Command.COMMAND_TAG.ATTACK] = true;
                    // コマンド選択に戻る
                    Back();

                    return true;
            }

            return false;
        }

        public override void Exit()
        {
            var btlUIInstance = BattleUISystem.Instance;

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
            _btlMgr.ResetDamageExpect(_attackCharacter, _targetCharacter);
            // アタックカーソルUI非表示
            btlUIInstance.ToggleAttackCursorP2E(false);
            // ダメージ予測表示UIを非表示
            btlUIInstance.ToggleBattleExpect(false);
            // 使用スキルの点滅を非表示
            for (int i = 0; i < Constants.EQUIPABLE_SKILL_MAX_NUM; ++i)
            {
                btlUIInstance.GetPlayerParamSkillBox(i).SetFlickEnabled(false);
                btlUIInstance.GetPlayerParamSkillBox(i).SetUseable(true);
                btlUIInstance.GetEnemyParamSkillBox(i).SetFlickEnabled(false);
                btlUIInstance.GetEnemyParamSkillBox(i).SetUseable(true);
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
    }
}