using Frontier.Combat;
using Frontier.Stage;
using Frontier.StateMachine;
using Frontier.Entities;
using UnityEngine;
using Frontier.Combat.Skill;
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

        private EmAttackPhase _phase;
        private int _curentGridIndex                    = -1;
        private string[] _playerSkillNames              = null;
        private Enemy _attackCharacter                  = null;
        private Character _targetCharacter              = null;
        private CharacterAttackSequence _attackSequence = null;

        public override void Init()
        {
            base.Init();

            _attackSequence     = _hierarchyBld.InstantiateWithDiContainer<CharacterAttackSequence>(false);
            _curentGridIndex    = _stageCtrl.GetCurrentGridIndex();
            _attackCharacter    = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter() as Enemy;
            Debug.Assert(_attackCharacter != null);

            // 現在選択中のキャラクター情報を取得して攻撃範囲を表示
            _attackCharacter.ActionRangeCtrl.SetupAttackableRangeData( _attackCharacter.Params.TmpParam.gridIndex );
            _attackCharacter.ActionRangeCtrl.DrawAttackableRange();

            // 攻撃可能なタイル内に攻撃可能対象がいた場合にグリッドを合わせる
            if( _stageCtrl.TileDataHdlr().CorrectAttackableTileIndexs( _attackCharacter, _attackCharacter.GetAi().GetTargetCharacter() ) )
            {
                _stageCtrl.BindToGridCursor( GridCursorState.ATTACK, _attackCharacter );    // アタッカーキャラクターの設定
				_uiSystem.BattleUi.SetAttackCursorE2PActive( true );                           // アタックカーソルUI表示
			}

            _targetCharacter = _attackCharacter.GetAi().GetTargetCharacter();
            _stageCtrl.ApplyCurrentGrid2CharacterTile(_attackCharacter);

            _playerSkillNames = _targetCharacter.Params.CharacterParam.GetEquipSkillNames();

            // 攻撃者の向きを設定
            var targetTileData = _stageCtrl.GetTileStaticData( _targetCharacter.Params.TmpParam.GetCurrentGridIndex() );
            _attackCharacter.GetTransformHandler.RotateToPosition( targetTileData.CharaStandPos );
            var attackerTileData = _stageCtrl.GetTileStaticData( _attackCharacter.Params.TmpParam.GetCurrentGridIndex() );
            _targetCharacter.GetTransformHandler.RotateToPosition( attackerTileData.CharaStandPos );

            // 攻撃シーケンスを初期化
            _attackSequence.Init();

            _phase = EmAttackPhase.EM_ATTACK_CONFIRM;
        }

        public override bool Update()
        {
            // 攻撃可能状態でなければ何もしない
            if (_stageCtrl.GetGridCursorControllerState() != GridCursorState.ATTACK)
            {
                return false;
            }

            switch (_phase)
            {
                case EmAttackPhase.EM_ATTACK_CONFIRM:
                    // 使用スキルを選択する
                    _attackCharacter.SelectUseSkills(SituationType.ATTACK);
                    _targetCharacter.SelectUseSkills(SituationType.DEFENCE);

                    // 予測ダメージを適応する
                    _btlRtnCtrl.BtlCharaCdr.ApplyDamageExpect(_attackCharacter, _targetCharacter);

                    // ダメージ予測表示UIを表示
                    _uiSystem.BattleUi.ToggleBattleExpect(true);

                    break;
                case EmAttackPhase.EM_ATTACK_EXECUTE:
                    if (_attackSequence.Update())
                    {
                        _phase = EmAttackPhase.EM_ATTACK_END;
                    }
                    break;
                case EmAttackPhase.EM_ATTACK_END:
                    // 攻撃したキャラクターの攻撃コマンドを選択不可にする
                    _attackCharacter.Params.TmpParam.SetEndCommandStatus( COMMAND_TAG.ATTACK, true );
                    // コマンド選択に戻る
                    Back();

                    return true;
            }

            return false;
        }

        public override void ExitState()
        {
            //死亡判定を通知(相手のカウンターによって倒される可能性もあるため、攻撃者と被攻撃者の両方を判定)
            Character diedCharacter = _attackSequence.GetDiedCharacter();
            if( diedCharacter != null )
            {
                var key = new CharacterKey( diedCharacter.Params.CharacterParam.characterTag, diedCharacter.Params.CharacterParam.characterIndex );
                NorifyCharacterDied( key );
                // 破棄
                diedCharacter.Dispose();
            }

            // アタッカーキャラクターの設定を解除
            _stageCtrl.ClearGridCursroBind();
            // 予測ダメージをリセット
            _attackCharacter.Params.TmpParam.SetExpectedHpChange(0, 0);
            _targetCharacter.Params.TmpParam.SetExpectedHpChange(0, 0);

            // アタックカーソルUI非表示
            _uiSystem.BattleUi.SetAttackCursorP2EActive(false);
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
            _attackCharacter.Params.CharacterParam.ResetConsumptionActionGauge();
            _attackCharacter.Params.SkillModifiedParam.Reset();
            _targetCharacter.Params.CharacterParam.ResetConsumptionActionGauge();
            _targetCharacter.Params.SkillModifiedParam.Reset();
            _btlRtnCtrl.BtlCharaCdr.ClearAllTileMeshes(); // タイルメッシュの描画をすべてクリア
            // 選択グリッドを表示
            // ※この攻撃の直後にプレイヤーフェーズに移行した場合、一瞬の間、選択グリッドが表示され、
            //   その後プレイヤーに選択グリッドが移るという状況になります。
            //   その挙動が少しバグのように見えてしまうので、消去したままにすることにし、
            //   次のキャラクターが行動開始する際に表示するようにします。
            // Stage.StageController.Instance.SetGridCursorControllerActive(true);

            base.ExitState();
        }

        public override void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            _inputFcd.RegisterInputCodes(
               (GuideIcon.CONFIRM,  "Confirm",              CanAcceptConfirm, new AcceptBooleanInput(AcceptConfirm), 0.0f, hashCode),
               (GuideIcon.SUB1,     _playerSkillNames[0],   CanAcceptSub1, new AcceptBooleanInput(AcceptSub1), 0.0f, hashCode),
               (GuideIcon.SUB2,     _playerSkillNames[1],   CanAcceptSub2, new AcceptBooleanInput(AcceptSub2), 0.0f, hashCode),
               (GuideIcon.SUB3,     _playerSkillNames[2],   CanAcceptSub3, new AcceptBooleanInput(AcceptSub3), 0.0f, hashCode),
               (GuideIcon.SUB4,     _playerSkillNames[3],   CanAcceptSub4, new AcceptBooleanInput(AcceptSub4), 0.0f, hashCode)
            );
        }

        /// <summary>
        /// 決定入力受付の可否を判定します
        /// </summary>
        /// <returns>決定入力受付の可否</returns>
        override protected bool CanAcceptConfirm()
        {
            if( !CanAcceptDefault() ) return false;

            if( EmAttackPhase.EM_ATTACK_CONFIRM == _phase ) return true;

            return false;
        }

        /// <summary>
        /// サブ1の入力の受付可否を判定します
        /// </summary>
        /// <returns>サブ1の入力の受付可否</returns>
        override protected bool CanAcceptSub1()
        {
            if (!CanAcceptDefault()) return false;

            if (EmAttackPhase.EM_ATTACK_CONFIRM != _phase) return false;

            if (_playerSkillNames[0].Length <= 0) return false;

            return _targetCharacter.CanToggleEquipSkill(0, SituationType.DEFENCE);
        }

        override protected bool CanAcceptSub2()
        {
            if (!CanAcceptDefault()) return false;

            if (EmAttackPhase.EM_ATTACK_CONFIRM != _phase) return false;

            if (_playerSkillNames[1].Length <= 0) return false;

            return _targetCharacter.CanToggleEquipSkill(1, SituationType.DEFENCE);
        }

        override protected bool CanAcceptSub3()
        {
            if (!CanAcceptDefault()) return false;

            if (EmAttackPhase.EM_ATTACK_CONFIRM != _phase) return false;

            if (_playerSkillNames[2].Length <= 0) return false;

            return _targetCharacter.CanToggleEquipSkill(2, SituationType.DEFENCE);
        }

        override protected bool CanAcceptSub4()
        {
            if (!CanAcceptDefault()) return false;

            if (EmAttackPhase.EM_ATTACK_CONFIRM != _phase) return false;

            if (_playerSkillNames[3].Length <= 0) return false;

            return _targetCharacter.CanToggleEquipSkill(3, SituationType.DEFENCE);
        }

        /// <summary>
        /// 決定入力を受け取った際の処理を行います
        /// </summary>
        /// <param name="isConfirm">決定入力の有無</param>
        override protected bool AcceptConfirm( bool isInput )
        {
            if( !isInput ) return false;

            // キャラクターのアクションゲージを消費
            _attackCharacter.ConsumeActionGauge();
            _targetCharacter.ConsumeActionGauge();

            // 選択グリッドを一時非表示
            _stageCtrl.SetGridCursorControllerActive(false);

            // アタックカーソルUI非表示
            _uiSystem.BattleUi.SetAttackCursorE2PActive(false);

            // ダメージ予測表示UIを非表示
            _uiSystem.BattleUi.ToggleBattleExpect(false);

            _btlRtnCtrl.BtlCharaCdr.ClearAllTileMeshes(); // タイルメッシュの描画をすべてクリア

            // 攻撃シーケンスの開始
            _attackSequence.StartSequence(_attackCharacter, _targetCharacter);

            _phase = EmAttackPhase.EM_ATTACK_EXECUTE;

            return true;
        }

        override protected bool AcceptSub1(bool isInput)
        {
            if (!isInput) return false;

            return _targetCharacter.ToggleUseSkillks(0);
        }

        override protected bool AcceptSub2(bool isInput)
        {
            if (!isInput) return false;

            return _targetCharacter.ToggleUseSkillks(1);
        }

        override protected bool AcceptSub3(bool isInput)
        {
            if (!isInput) return false;

            return _targetCharacter.ToggleUseSkillks(2);
        }

        override protected bool AcceptSub4(bool isInput)
        {
            if (!isInput) return false;

            return _targetCharacter.ToggleUseSkillks(3);
        }
    }
}