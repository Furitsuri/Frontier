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

        // ���ݑI�𒆂̃L�����N�^�[�����擾���čU���͈͂�\��
        attackCharacter = btlInstance.SearchPlayerFromCharaIndex(btlInstance.SelectCharacterIndex);
        if (attackCharacter == null)
        {
            Debug.Assert(false, "SelectPlayer Irregular.");
            return;
        }
        var param = attackCharacter.param;
        stgInstance.DrawAttackableGrids(curentGridIndex, param.attackRangeMin, param.attackRangeMax);

        // �U���\�ȃO���b�h���ɓG�������ꍇ�ɕW�I�O���b�h�����킹��
        stgInstance.ApplyAttackTargetGridIndexs(0);
        // �A�^�b�J�[�L�����N�^�[�̐ݒ�
        btlInstance.SetAttackerCharacter( attackCharacter );
    }

    public override void Update()
    {
        var btlInstance = BattleManager.instance;

        base.Update();

        switch(m_Phase)
        {
            case PLAttackPhase.PL_ATTACK_SELECT_GRID:
                // �O���b�h�̑���
                StageGrid.instance.OperateCurrentGrid();

                if (Input.GetKeyUp(KeyCode.Space))
                {
                    // �I�������L�����N�^�[���G�ł���ꍇ�͍U���J�n
                    var character = btlInstance.GetSelectCharacter();
                    if( character != null && character.param.charaTag == Character.CHARACTER_TAG.CHARACTER_ENEMY )
                    {
                        // targetCharacter�ɑ��
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
                // �A�^�b�J�[�L�����N�^�[�̐ݒ������
                btlInstance.ResetAttackerCharacter();

                break;

        }
    }
}
