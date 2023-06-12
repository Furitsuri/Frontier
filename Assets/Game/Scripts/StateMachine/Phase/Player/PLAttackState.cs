using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static BattleManager;

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
    private List<int> _attackableGrids = new List<int>(64);
    private CharacterAttackSequence _attackSequence = new CharacterAttackSequence();

    override public void Init()
    {
        var stgInstance = StageGrid.instance;
        var btlInstance = BattleManager.instance;

        base.Init();

        _phase         = PLAttackPhase.PL_ATTACK_SELECT_GRID;
        _curentGridIndex = stgInstance.currentGrid.GetIndex();

        // 現在選択中のキャラクター情報を取得して攻撃範囲を表示
        _attackCharacter = btlInstance.SearchPlayerFromCharaIndex(btlInstance.SelectCharacterIndex);
        if (_attackCharacter == null)
        {
            Debug.Assert(false, "SelectPlayer Irregular.");
            return;
        }
        var param = _attackCharacter.param;
        stgInstance.DrawAttackableGrids(_curentGridIndex, param.attackRangeMin, param.attackRangeMax);

        // 攻撃可能なグリッド内に敵がいた場合に標的グリッドを合わせる
        stgInstance.ApplyAttackTargetGridIndexs(0);
        // アタッカーキャラクターの設定
        btlInstance.SetAttackerCharacter( _attackCharacter );
    }

    public override void Update()
    {
        var btlInstance = BattleManager.instance;

        base.Update();

        switch(_phase)
        {
            case PLAttackPhase.PL_ATTACK_SELECT_GRID:
                // グリッドの操作
                StageGrid.instance.OperateCurrentGrid();

                if (Input.GetKeyUp(KeyCode.Space))
                {
                    // 選択したキャラクターが敵である場合は攻撃開始
                    var character = btlInstance.GetSelectCharacter();
                    if( character != null && character.param.charaTag == Character.CHARACTER_TAG.CHARACTER_ENEMY )
                    {
                        // _targetCharacterに代入
                        _targetCharacter = character;
                        _attackSequence.Init(_attackCharacter, _targetCharacter);

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
                // アタッカーキャラクターの設定を解除
                btlInstance.ResetAttackerCharacter();

                break;

        }
    }
}
