using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        _phase         = PLAttackPhase.PL_ATTACK_SELECT_GRID;
        _curentGridIndex = stgInstance.currentGrid.GetIndex();

        // ���ݑI�𒆂̃L�����N�^�[�����擾���čU���͈͂�\��
        _attackCharacter = btlInstance.SearchPlayerFromCharaIndex(btlInstance.SelectCharacterIndex);
        if (_attackCharacter == null)
        {
            Debug.Assert(false, "SelectPlayer Irregular.");
            return;
        }
        var param = _attackCharacter.param;
        stgInstance.DrawAttackableGrids(_curentGridIndex, param.attackRangeMin, param.attackRangeMax);

        // �U���\�ȃO���b�h���ɓG�������ꍇ�ɕW�I�O���b�h�����킹��
        if( stgInstance.RegistAttackTargetGridIndexs(Character.CHARACTER_TAG.CHARACTER_ENEMY) )
        {
            // �A�^�b�J�[�L�����N�^�[�̐ݒ�
            btlInstance.SetAttackerCharacter(_attackCharacter);
            // �A�^�b�N�J�[�\��UI�\��
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

        // �U���\��ԂłȂ���Ή������Ȃ�
        if(!btlInstance.IsAttackPhaseState())
        {
            return false;
        }

        switch(_phase)
        {
            case PLAttackPhase.PL_ATTACK_SELECT_GRID:
                // �O���b�h�̑���
                StageGrid.Instance.OperateCurrentGrid();
                // �O���b�h��̃L�����N�^�[���擾
                _targetCharacter = btlInstance.GetSelectCharacter();
                // �\���_���[�W��K������
                btlInstance.ApplyDamageExpect(_attackCharacter, _targetCharacter);
                // �_���[�W�\���\��UI��\��
                btlUIInstance.ToggleBattleExpect(true);

                if (Input.GetKeyUp(KeyCode.Space))
                {
                    // �I�������L�����N�^�[���G�ł���ꍇ�͍U���J�n
                    if( _targetCharacter != null && _targetCharacter.param.charaTag == Character.CHARACTER_TAG.CHARACTER_ENEMY )
                    {
                        // �U���V�[�P���X��������
                        _attackSequence.Init(_attackCharacter, _targetCharacter);
                        // �A�^�b�N�J�[�\��UI��\��
                        btlUIInstance.ToggleAttackCursorP2E(false);
                        // �_���[�W�\���\��UI���\��
                        btlUIInstance.ToggleBattleExpect(false);
                        // �O���b�h��Ԃ̕`����N���A
                        stgInstance.ClearGridsCondition();

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
                // �U�������L�����N�^�[�̍U���R�}���h��I��s�ɂ���
                _attackCharacter.tmpParam.isEndCommand[(int)Character.BaseCommand.COMMAND_ATTACK] = true;
                // �R�}���h�I���ɖ߂�
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
        // �A�^�b�J�[�L�����N�^�[�̐ݒ������
        btlInstance.ResetAttackerCharacter();
        // �\���_���[�W�����Z�b�g
        btlInstance.ResetDamageExpect(_attackCharacter, _targetCharacter);
        // �A�^�b�N�J�[�\��UI��\��
        btlUIInstance.ToggleAttackCursorP2E(false);
        // �_���[�W�\���\��UI���\��
        btlUIInstance.ToggleBattleExpect(false);
        // �O���b�h��Ԃ̕`����N���A
        stgInstance.ClearGridsCondition();

        base.Exit();
    }
}
