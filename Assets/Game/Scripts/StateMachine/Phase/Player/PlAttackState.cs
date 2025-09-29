using Frontier.Combat;
using Frontier.Stage;
using Frontier.Entities;
using Frontier.Combat.Skill;
using static Constants;

namespace Frontier
{
    public class PlAttackState : PlPhaseStateBase
    {
        protected enum PlAttackPhase
        {
            PL_ATTACK_SELECT_GRID = 0,
            PL_ATTACK_EXECUTE,
            PL_ATTACK_END,
        }

        protected PlAttackPhase _phase = PlAttackPhase.PL_ATTACK_SELECT_GRID;
        protected int _curentGridIndex = -1;
        protected string[] _playerSkillNames = null;
        protected Character _attackCharacter = null;
        protected Character _targetCharacter = null;
        protected CharacterAttackSequence _attackSequence = null;

        protected void PlPhaseStateInit()
        {
            base.Init();
        }

        /// <summary>
        /// 初期化します
        /// </summary>
        override public void Init()
        {
            base.Init();

            _playerSkillNames   = _plOwner.Params.CharacterParam.GetEquipSkillNames();
            _attackSequence     = _hierarchyBld.InstantiateWithDiContainer<CharacterAttackSequence>(false);
            _phase              = PlAttackPhase.PL_ATTACK_SELECT_GRID;
            _curentGridIndex    = _stageCtrl.GetCurrentGridIndex();
            _targetCharacter    = null;

            // 現在選択中のキャラクター情報を取得して攻撃範囲を表示
            _attackCharacter = _plOwner;
            var param = _attackCharacter.Params.CharacterParam;
            _stageCtrl.BeginRegisterAttackableInformation(_curentGridIndex, param.attackRange, param.characterTag);
            _stageCtrl.DrawAllTileInformationMeshes();

            // 攻撃可能なグリッド内に敵がいた場合に標的グリッドを合わせる
            if (_stageCtrl.RegisterAttackableTileIndexs(CHARACTER_TAG.ENEMY, _btlRtnCtrl.BtlCharaCdr.GetNearestLineOfSightCharacter( _attackCharacter, CHARACTER_TAG.ENEMY )))
            {
                _stageCtrl.BindToGridCursor( GridCursorState.ATTACK, _attackCharacter);  // アタッカーキャラクターの設定
                _uiSystem.BattleUi.ToggleAttackCursorP2E(true); // アタックカーソルUI表示
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
            if (_stageCtrl.GetGridCursorControllerState() != GridCursorState.ATTACK)
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
                        var targetGridInfo = _stageCtrl.GetTileInfo(_targetCharacter.Params.TmpParam.GetCurrentGridIndex());
                        _attackCharacter.GetTransformHandler.RotateToPosition(targetGridInfo.charaStandPos );
                        var attackerGridInfo = _stageCtrl.GetTileInfo(_attackCharacter.Params.TmpParam.GetCurrentGridIndex());
                        _targetCharacter.GetTransformHandler.RotateToPosition(attackerGridInfo.charaStandPos);
                    }

                    // ダメージ予測表示UIを表示
                    _uiSystem.BattleUi.ToggleBattleExpect(true);

                    // 使用スキルを選択する
                    _attackCharacter.SelectUseSkills(SituationType.ATTACK);
                    _targetCharacter.SelectUseSkills(SituationType.DEFENCE);

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
                    _attackCharacter.Params.TmpParam.SetEndCommandStatus( Command.COMMAND_TAG.ATTACK, true );
                    // コマンド選択に戻る
                    Back();

                    return true;
            }

            return false;
        }

        override public void ExitState()
        {
            //死亡判定を通知(相手のカウンターによって倒される可能性もあるため、攻撃者と被攻撃者の両方を判定)
            Character diedCharacter = _attackSequence.GetDiedCharacter();
            if (diedCharacter != null)
            {
                var key = new CharacterHashtable.Key(diedCharacter.Params.CharacterParam.characterTag, diedCharacter.Params.CharacterParam.characterIndex);
                NorifyCharacterDied(key);
                // 破棄
                diedCharacter.Dispose();
            }

            // アタッカーキャラクターの設定を解除
            _stageCtrl.ClearGridCursroBind();

            // 予測ダメージをリセット
            _attackCharacter.Params.TmpParam.SetExpectedHpChange(0, 0);
            _targetCharacter.Params.TmpParam.SetExpectedHpChange(0, 0);

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
            _attackCharacter.Params.CharacterParam.ResetConsumptionActionGauge();
            _attackCharacter.Params.SkillModifiedParam.Reset();
            _targetCharacter.Params.CharacterParam.ResetConsumptionActionGauge();
            _targetCharacter.Params.SkillModifiedParam.Reset();

            // グリッドの状態を更新してグリッドの描画をクリア
            _stageCtrl.UpdateTileInfo();
            _stageCtrl.ClearGridMeshDraw();

            // 選択グリッドを表示
            _stageCtrl.SetGridCursorControllerActive(true);

            base.ExitState();
        }

        /// <summary>
        /// 入力コードを登録します
        /// </summary>
        override public void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            // 入力ガイドを登録
            _inputFcd.RegisterInputCodes(
               (GuideIcon.ALL_CURSOR, "TARGET SELECT",  CanAcceptDirection, new AcceptDirectionInput(AcceptDirection), MENU_DIRECTION_INPUT_INTERVAL, hashCode),
               (GuideIcon.CONFIRM, "CONFIRM",           CanAcceptConfirm, new AcceptBooleanInput(AcceptConfirm), 0.0f, hashCode),
               (GuideIcon.CANCEL, "BACK",               CanAcceptCancel, new AcceptBooleanInput(AcceptCancel), 0.0f, hashCode),
               (GuideIcon.SUB1, _playerSkillNames[0],   CanAcceptSub1, new AcceptBooleanInput(AcceptSub1), 0.0f, hashCode),
               (GuideIcon.SUB2, _playerSkillNames[1],   CanAcceptSub2, new AcceptBooleanInput(AcceptSub2), 0.0f, hashCode),
               (GuideIcon.SUB3, _playerSkillNames[2],   CanAcceptSub3, new AcceptBooleanInput(AcceptSub3), 0.0f, hashCode),
               (GuideIcon.SUB4, _playerSkillNames[3],   CanAcceptSub4, new AcceptBooleanInput(AcceptSub4), 0.0f, hashCode)
            );
        }

        /// <summary>
        /// 操作対象のプレイヤーを設定します
        /// </summary>
        override protected void AdaptSelectPlayer()
        {
            // グリッドカーソルで選択中のプレイヤーを取得
            _plOwner = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter() as Player;
            NullCheck.AssertNotNull( _plOwner, nameof( _plOwner ) );
        }

        /// <summary>
        /// 方向入力の受付可否を判定します
        /// </summary>
        /// <returns>方向入力の受付可否</returns>
        override protected bool CanAcceptDirection()
        {
            if ( !CanAcceptDefault() )                              { return false; }
            if ( PlAttackPhase.PL_ATTACK_SELECT_GRID != _phase )    { return false; }   // 攻撃対象選択フェーズでない場合は終了
            if ( _stageCtrl.GetAttackabkeTargetNum() <= 1 )         { return false; }   // 攻撃可能な標的数が1より大きくなければ終了

            return true;
        }

        /// <summary>
        /// 決定入力の受付可否を判定します
        /// </summary>
        /// <returns>決定入力の受付可否</returns>
        override protected bool CanAcceptConfirm()
        {
            if ( !CanAcceptDefault() ) { return false; }
            if ( PlAttackPhase.PL_ATTACK_SELECT_GRID != _phase ) { return false; }   // 攻撃対象選択フェーズでない場合は終了

            return true;
        }

        /// <summary>
        /// キャンセル入力の受付可否を判定します
        /// </summary>
        /// <returns>キャンセル入力の受付可否</returns>
        override protected bool CanAcceptCancel()
        {
            // Confirmと同一
            return CanAcceptConfirm();
        }

        /// <summary>
        /// サブ1の入力の受付可否を判定します
        /// </summary>
        /// <returns>サブ1の入力の受付可否</returns>
        override protected bool CanAcceptSub1()
        {
            if (!CanAcceptConfirm()) return false;

            if (_playerSkillNames[0].Length <= 0 ) return false;

            return _plOwner.CanToggleEquipSkill(0, SituationType.ATTACK);
        }

        override protected bool CanAcceptSub2()
        {
            if (!CanAcceptConfirm()) return false;

            if (_playerSkillNames[1].Length <= 0) return false;

            return _plOwner.CanToggleEquipSkill(1, SituationType.ATTACK);
        }

        override protected bool CanAcceptSub3()
        {
            if (!CanAcceptConfirm()) return false;

            if (_playerSkillNames[2].Length <= 0) return false;

            return _plOwner.CanToggleEquipSkill(2, SituationType.ATTACK);
        }

        override protected bool CanAcceptSub4()
        {
            if (!CanAcceptConfirm()) return false;

            if (_playerSkillNames[3].Length <= 0) return false;

            return _plOwner.CanToggleEquipSkill(3, SituationType.ATTACK);
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
            if (_targetCharacter != null && _targetCharacter.Params.CharacterParam.characterTag == CHARACTER_TAG.ENEMY)
            {
                // キャラクターのアクションゲージを消費
                _attackCharacter.ConsumeActionGauge();
                _targetCharacter.ConsumeActionGauge();

                // 選択グリッドを一時非表示
                _stageCtrl.SetGridCursorControllerActive(false);

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

        override protected bool AcceptSub1(bool isInput)
        {
            if ( !isInput ) return false;

            return _plOwner.ToggleUseSkillks(0);
        }

        override protected bool AcceptSub2(bool isInput)
        {
            if ( !isInput ) return false;

            return _plOwner.ToggleUseSkillks(1);
        }

        override protected bool AcceptSub3(bool isInput)
        {
            if ( !isInput ) return false;

            return _plOwner.ToggleUseSkillks(2);
        }

        override protected bool AcceptSub4(bool isInput)
        {
            if ( !isInput ) return false;

            return _plOwner.ToggleUseSkillks(3);
        }
    }
}