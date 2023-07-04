using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class EMAttackState : PhaseStateBase
{
    private enum EMAttackPhase
    {
        EM_ATTACK_CONFIRM = 0,
        EM_ATTACK_EXECUTE,
        EM_ATTACK_END,
    }

    private EMAttackPhase _phase = EMAttackPhase.EM_ATTACK_EXECUTE;
    private Enemy _enemy = null;
    private Character _targetCharacter = null;
    private int _curentGridIndex = -1;
    private CharacterAttackSequence _attackSequence = new CharacterAttackSequence();

    public override void Init()
    {
        var stgInstance     = StageGrid.Instance;
        var btlInstance     = BattleManager.Instance;
        var btlUIInstance   = BattleUISystem.Instance;

        base.Init();

        _phase              = EMAttackPhase.EM_ATTACK_CONFIRM;
        _curentGridIndex    = stgInstance.GetCurrentGridIndex();
        _enemy              = btlInstance.GetSelectCharacter() as Enemy;
        Debug.Assert(_enemy != null);

        // 現在選択中のキャラクター情報を取得して攻撃範囲を表示
        var param = _enemy.param;
        stgInstance.RegistAttackAbleInfo(_curentGridIndex, param.attackRange, param.characterTag);
        stgInstance.DrawAttackableGrids(_curentGridIndex);

        // 攻撃可能なグリッド内に敵がいた場合に標的グリッドを合わせる
        if (stgInstance.RegistAttackTargetGridIndexs(Character.CHARACTER_TAG.CHARACTER_PLAYER))
        {
            // アタッカーキャラクターの設定
            btlInstance.SetAttackerCharacter(_enemy);
            // アタックカーソルUI表示
            btlUIInstance.ToggleAttackCursorE2P(true);
        }

        _targetCharacter = _enemy.EmAI.GetTargetCharacter();
        stgInstance.ApplyCurrentGrid2CharacterGrid(_enemy);
        // 予測ダメージを適応する
        btlInstance.ApplyDamageExpect(_enemy, _targetCharacter);
        // ダメージ予測表示UIを表示
        btlUIInstance.ToggleBattleExpect(true);

        _phase = EMAttackPhase.EM_ATTACK_CONFIRM;
    }

    public override bool Update()
    {
        var stgInstance = StageGrid.Instance;
        var btlInstance = BattleManager.Instance;
        var btlUIInstance = BattleUISystem.Instance;

        // 攻撃可能状態でなければ何もしない
        if (!btlInstance.IsAttackPhaseState())
        {
            return false;
        }

        switch (_phase)
        {
            case EMAttackPhase.EM_ATTACK_CONFIRM:
                if(Input.GetKeyUp(KeyCode.Space))
                {
                    // 選択グリッドを一時非表示
                    BattleUISystem.Instance.ToggleSelectGrid(false);
                    // 攻撃シーケンスを初期化
                    _attackSequence.Init(_enemy, _targetCharacter);
                    // アタックカーソルUI非表示
                    btlUIInstance.ToggleAttackCursorE2P(false);
                    // ダメージ予測表示UIを非表示
                    btlUIInstance.ToggleBattleExpect(false);
                    // グリッド状態の描画をクリア
                    stgInstance.ClearGridMeshDraw();

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
                _enemy.tmpParam.isEndCommand[(int)Character.BaseCommand.COMMAND_ATTACK] = true;
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
        btlInstance.ResetDamageExpect(_enemy, _targetCharacter);
        // アタックカーソルUI非表示
        btlUIInstance.ToggleAttackCursorP2E(false);
        // ダメージ予測表示UIを非表示
        btlUIInstance.ToggleBattleExpect(false);
        // グリッド状態の描画をクリア
        stgInstance.UpdateGridInfo();
        stgInstance.ClearGridMeshDraw();
        // 選択グリッドを表示
        // ※この攻撃の直後にプレイヤーフェーズに移行した場合、一瞬の間、選択グリッドが表示され、
        //   その後プレイヤーに選択グリッドが移るという状況になります。
        //   その挙動が少しバグのように見えてしまうので、消去したままにすることにし、
        //   次のキャラクターが行動開始する際に表示するようにします。
        // BattleUISystem.Instance.ToggleSelectGrid(true);

        base.Exit();
    }
}
