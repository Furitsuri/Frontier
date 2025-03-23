using Frontier.Combat;
using Frontier.Stage;
using Frontier.Entities;
using System.Collections.Generic;
using UnityEngine;
using static Constants;

namespace Frontier
{
    public class PlAttackState : PlPhaseStateBase
    {
        private enum PlAttackPhase
        {
            PL_ATTACK_SELECT_GRID = 0,
            PL_ATTACK_EXECUTE,
            PL_ATTACK_END,
        }

        private PlAttackPhase _phase = PlAttackPhase.PL_ATTACK_SELECT_GRID;
        private int _curentGridIndex = -1;
        private string[] _playerSkillNames = null;
        private Character _attackCharacter = null;
        private Character _targetCharacter = null;
        private CharacterAttackSequence _attackSequence = null;

        /// <summary>
        /// 初期化します
        /// </summary>
        override public void Init()
        {
            base.Init();

            _playerSkillNames = _selectPlayer.GetEquipSkillNames();

            // 入力ガイドを登録
            _inputFcd.RegisterInputCodes(
               (GuideIcon.ALL_CURSOR,   "TARGET SELECT",        CanAcceptDirection, new AcceptDirectionInput(AcceptDirection), DIRECTION_INPUT_INTERVAL),
               (GuideIcon.CONFIRM,      "CONFIRM",              CanAcceptDirection, new AcceptBooleanInput(AcceptConfirm), 0.0f),
               (GuideIcon.CANCEL,       "TURN END",             CanAcceptCancel,    new AcceptBooleanInput(AcceptCancel), 0.0f),
               (GuideIcon.SUB1,         _playerSkillNames[0],   CanAcceptSub1,      new AcceptBooleanInput(AcceptSub1), 0.0f),
               (GuideIcon.SUB2,         _playerSkillNames[1],   CanAcceptSub2,      new AcceptBooleanInput(AcceptSub2), 0.0f),
               (GuideIcon.SUB3,         _playerSkillNames[2],   CanAcceptSub3,      new AcceptBooleanInput(AcceptSub3), 0.0f),
               (GuideIcon.SUB4,         _playerSkillNames[3],   CanAcceptSub4,      new AcceptBooleanInput(AcceptSub4), 0.0f)
            );

            _attackSequence     = _hierarchyBld.InstantiateWithDiContainer<CharacterAttackSequence>();
            _phase              = PlAttackPhase.PL_ATTACK_SELECT_GRID;
            _curentGridIndex    = _stageCtrl.GetCurrentGridIndex();
            _targetCharacter    = null;

            // 現在選択中のキャラクター情報を取得して攻撃範囲を表示
            _attackCharacter = _selectPlayer;
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
                case PlAttackPhase.PL_ATTACK_SELECT_GRID:
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

                    break;
                case PlAttackPhase.PL_ATTACK_EXECUTE:
                    if (_attackSequence.Update())
                    {
                        _phase = PlAttackPhase.PL_ATTACK_END;
                    }

                    break;
                case PlAttackPhase.PL_ATTACK_END:
                    // 攻撃したキャラクターの攻撃コマンドを選択不可にする
                    _attackCharacter.SetEndCommandStatus( Command.COMMAND_TAG.ATTACK, true );
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
            if (PlAttackPhase.PL_ATTACK_SELECT_GRID == _phase) return true;

            return false;
        }

        /// <summary>
        /// 決定入力の受付可否を判定します
        /// </summary>
        /// <returns>決定入力の受付可否</returns>
        override protected bool CanAcceptConfirm()
        {
            // Directionと同一
            return CanAcceptDirection();
        }

        /// <summary>
        /// キャンセル入力の受付可否を判定します
        /// </summary>
        /// <returns>キャンセル入力の受付可否</returns>
        override protected bool CanAcceptCancel()
        {
            // Directionと同一
            return CanAcceptDirection();
        }

        /// <summary>
        /// サブ1の入力の受付可否を判定します
        /// </summary>
        /// <returns>サブ1の入力の受付可否</returns>
        protected override bool CanAcceptSub1()
        {
            if (!CanAcceptDirection()) return false;

            if (_playerSkillNames[0].Length <= 0 ) return false;

            return _selectPlayer.CanToggleEquipSkill(0);
        }

        protected override bool CanAcceptSub2()
        {
            if (!CanAcceptDirection()) return false;

            if (_playerSkillNames[1].Length <= 0) return false;

            return _selectPlayer.CanToggleEquipSkill(1);
        }

        protected override bool CanAcceptSub3()
        {
            if (!CanAcceptDirection()) return false;

            if (_playerSkillNames[2].Length <= 0) return false;

            return _selectPlayer.CanToggleEquipSkill(2);
        }

        protected override bool CanAcceptSub4()
        {
            if (!CanAcceptDirection()) return false;

            if (_playerSkillNames[3].Length <= 0) return false;

            return _selectPlayer.CanToggleEquipSkill(3);
        }

        /// <summary>
        /// 方向入力を受け取った際の処理を行います
        /// </summary>
        /// <param name="dir">方向入力</param>
        /// <returns>入力実行の有無</returns>
        override protected bool AcceptDirection(Constants.Direction dir)
        {
            if (_stageCtrl.OperateTargetSelect(dir))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 決定入力を受け取った際の処理を行います
        /// </summary>
        /// <param name="isInput">決定入力</param>
        /// <returns>入力実行の有無</returns>
        override protected bool AcceptConfirm( bool isInput )
        {
            if( !isInput ) return false;

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

                _phase = PlAttackPhase.PL_ATTACK_EXECUTE;

                return true;
            }

            return false;
        }

        protected override bool AcceptSub1(bool isInput)
        {
            if ( !isInput ) return false;

            return _selectPlayer.ToggleUseSkillks(0);
        }

        protected override bool AcceptSub2(bool isInput)
        {
            if ( !isInput ) return false;

            return _selectPlayer.ToggleUseSkillks(1);
        }

        protected override bool AcceptSub3(bool isInput)
        {
            if ( !isInput ) return false;

            return _selectPlayer.ToggleUseSkillks(2);
        }

        protected override bool AcceptSub4(bool isInput)
        {
            if ( !isInput ) return false;

            return _selectPlayer.ToggleUseSkillks(3);
        }
    }
}