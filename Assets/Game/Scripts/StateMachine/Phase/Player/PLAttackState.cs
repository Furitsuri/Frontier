using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SkillsData;

public class PLAttackState : PhaseStateBase
{
    private enum PLAttackPhase
    {
        PL_ATTACK_SELECT_GRID = 0,
        PL_ATTACK_EXECUTE,
        PL_ATTACK_END,
    }

    private PLAttackPhase _phase = PLAttackPhase.PL_ATTACK_SELECT_GRID;
    private Character _attackCharacter = null;
    private Character _targetCharacter = null;
    private int _curentGridIndex = -1;
    private CharacterAttackSequence _attackSequence = new CharacterAttackSequence();

    override public void Init()
    {
        var stgInstance = StageGrid.Instance;
        var btlInstance = BattleManager.Instance;
        var btlUIInstance = BattleUISystem.Instance;

        base.Init();

        _phase              = PLAttackPhase.PL_ATTACK_SELECT_GRID;
        _curentGridIndex    = stgInstance.GetCurrentGridIndex();

        // 現在選択中のキャラクター情報を取得して攻撃範囲を表示
        _attackCharacter = btlInstance.GetCharacterFromHashtable(btlInstance.SelectCharacterInfo);
        if (_attackCharacter == null)
        {
            Debug.Assert(false, "SelectPlayer Irregular.");
            return;
        }
        var param = _attackCharacter.param;
        stgInstance.RegistAttackAbleInfo(_curentGridIndex, param.attackRange, param.characterTag);
        stgInstance.DrawAttackableGrids(_curentGridIndex);

        // 攻撃可能なグリッド内に敵がいた場合に標的グリッドを合わせる
        if( stgInstance.RegistAttackTargetGridIndexs(Character.CHARACTER_TAG.CHARACTER_ENEMY) )
        {
            // アタッカーキャラクターの設定
            btlInstance.SetAttackerCharacter(_attackCharacter);
            // アタックカーソルUI表示
            btlUIInstance.ToggleAttackCursorP2E(true);
        }
    }

    public override bool Update()
    {
        var stgInstance     = StageGrid.Instance;
        var btlInstance     = BattleManager.Instance;
        var btlUIInstance   = BattleUISystem.Instance;

        if( base.Update() )
        {
            return  true;
        }

        // 攻撃可能状態でなければ何もしない
        if(!btlInstance.IsAttackPhaseState())
        {
            return false;
        }

        switch(_phase)
        {
            case PLAttackPhase.PL_ATTACK_SELECT_GRID:
                // グリッドの操作
                StageGrid.Instance.OperateCurrentGrid();
                // グリッド上のキャラクターを取得
                _targetCharacter = btlInstance.GetSelectCharacter();
                // ダメージ予測表示UIを表示
                btlUIInstance.ToggleBattleExpect(true);
                // 使用スキルを選択する
                _attackCharacter.SelectUseSkills(SituationType.ATTACK);
                _targetCharacter.SelectUseSkills(SituationType.DEFENCE);

                // 予測ダメージを適応する
                btlInstance.ApplyDamageExpect(_attackCharacter, _targetCharacter);

                if (Input.GetKeyUp(KeyCode.Space))
                {
                    // 選択したキャラクターが敵である場合は攻撃開始
                    if( _targetCharacter != null && _targetCharacter.param.characterTag == Character.CHARACTER_TAG.CHARACTER_ENEMY )
                    {
                        // 選択グリッドを一時非表示
                        BattleUISystem.Instance.ToggleSelectGrid(false);
                        // 攻撃シーケンスを初期化
                        _attackSequence.Init(_attackCharacter, _targetCharacter);
                        // アタックカーソルUI非表示
                        btlUIInstance.ToggleAttackCursorP2E(false);
                        // ダメージ予測表示UIを非表示
                        btlUIInstance.ToggleBattleExpect(false);
                        // グリッド状態の描画をクリア
                        stgInstance.ClearGridMeshDraw();

                        _phase = PLAttackPhase.PL_ATTACK_EXECUTE;
                    }
                }
                break;
            case PLAttackPhase.PL_ATTACK_EXECUTE:
                if ( _attackSequence.Update() )
                {
                    _phase = PLAttackPhase.PL_ATTACK_END;
                }
                
                break;
            case PLAttackPhase.PL_ATTACK_END:
                // 攻撃したキャラクターの攻撃コマンドを選択不可にする
                _attackCharacter.tmpParam.isEndCommand[(int)Character.BaseCommand.COMMAND_ATTACK] = true;
                // コマンド選択に戻る
                Back();

                return true;
        }

        return false;
    }

    public override void Exit()
    {
        var btlInstance     = BattleManager.Instance;
        var btlUIInstance   = BattleUISystem.Instance;
        var stgInstance     = StageGrid.Instance;

        //死亡判定を通知(相手のカウンターによって倒される可能性もあるため、両方判定)
        Character diedCharacter = _attackSequence.GetDiedCharacter();
        if (diedCharacter != null)
        {
            var key = new CharacterHashtable.Key(diedCharacter.param.characterTag, diedCharacter.param.characterIndex);
            NoticeCharacterDied(key);
            // 破棄
            diedCharacter.Remove();
        }

        // アタッカーキャラクターの設定を解除
        btlInstance.ResetAttackerCharacter();
        // 予測ダメージをリセット
        btlInstance.ResetDamageExpect(_attackCharacter, _targetCharacter);
        // アタックカーソルUI非表示
        btlUIInstance.ToggleAttackCursorP2E(false);
        // ダメージ予測表示UIを非表示
        btlUIInstance.ToggleBattleExpect(false);
        // 使用スキルの点滅を非表示
        for (int i = 0; i < Constants.EQUIPABLE_SKILL_MAX_NUM; ++i) {
            btlUIInstance.PlayerParameter.GetSkillBox(i).SetFlickEnabled(false);
            btlUIInstance.PlayerParameter.GetSkillBox(i).SetUseable(true);
            btlUIInstance.EnemyParameter.GetSkillBox(i).SetFlickEnabled(false);
            btlUIInstance.EnemyParameter.GetSkillBox(i).SetUseable(true);
        }
        // 使用スキルコスト見積もりをリセット
        _attackCharacter.param.ResetConsumptionActionGauge();
        _targetCharacter.param.ResetConsumptionActionGauge();
        // グリッド状態の描画をクリア
        stgInstance.UpdateGridInfo();
        stgInstance.ClearGridMeshDraw();
        // 選択グリッドを表示
        BattleUISystem.Instance.ToggleSelectGrid(true);

        base.Exit();
    }
}
