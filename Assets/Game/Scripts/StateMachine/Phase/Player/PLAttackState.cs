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

    private PLAttackPhase m_Phase = PLAttackPhase.PL_ATTACK_SELECT_GRID;
    private Character attackCharacter = null;
    private Character targetCharacter = null;
    private int curentGridIndex = -1;
    private List<int> attackableGrids = new List<int>(64);
    private CharacterAttackSequence attackSequence = null;
    List<Vector3> moveGridPos;
    Transform PLTransform;
    override public void Init()
    {
        var stgInstance = StageGrid.instance;
        var btlInstance = BattleManager.instance;

        base.Init();

        m_Phase         = PLAttackPhase.PL_ATTACK_SELECT_GRID;
        curentGridIndex = stgInstance.CurrentGridIndex;

        // 現在選択中のキャラクター情報を取得して攻撃範囲を表示
        attackCharacter = btlInstance.SearchPlayerFromCharaIndex(btlInstance.SelectCharacterIndex);
        if (attackCharacter == null)
        {
            Debug.Assert(false, "SelectPlayer Irregular.");
            return;
        }
        var param = attackCharacter.param;
        stgInstance.DrawAttackableGrids(curentGridIndex, param.attackRangeMin, param.attackRangeMax);

        // 攻撃可能なグリッド内に敵がいた場合に標的グリッドを合わせる
        stgInstance.ApplyAttackTargetGridIndexs(0);
        // アタッカーキャラクターの設定
        btlInstance.SetAttackerCharacter( attackCharacter );
    }

    public override void Update()
    {
        var btlInstance = BattleManager.instance;

        base.Update();

        switch(m_Phase)
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
                        // targetCharacterに代入
                        targetCharacter = character;
                        attackSequence = new CharacterAttackSequence(attackCharacter, targetCharacter);
                    }
                }
                break;
            case PLAttackPhase.PL_ATTACK_EXECUTE:

                if ( attackSequence.Update() )
                {
                    m_Phase = PLAttackPhase.PL_ATTACK_END;
                }
                
                break;
            case PLAttackPhase.PL_ATTACK_END:
                // アタッカーキャラクターの設定を解除
                btlInstance.ResetAttackerCharacter();

                break;

        }
    }
}
