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

        // ���ݑI�𒆂̃L�����N�^�[�����擾���čU���͈͂�\��
        _attackCharacter = btlInstance.GetCharacterFromHashtable(btlInstance.SelectCharacterInfo);
        if (_attackCharacter == null)
        {
            Debug.Assert(false, "SelectPlayer Irregular.");
            return;
        }
        var param = _attackCharacter.param;
        stgInstance.RegistAttackAbleInfo(_curentGridIndex, param.attackRange, param.characterTag);
        stgInstance.DrawAttackableGrids(_curentGridIndex);

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
                // �_���[�W�\���\��UI��\��
                btlUIInstance.ToggleBattleExpect(true);
                // �g�p�X�L����I������
                _attackCharacter.SelectUseSkills(SituationType.ATTACK);
                _targetCharacter.SelectUseSkills(SituationType.DEFENCE);

                // �\���_���[�W��K������
                btlInstance.ApplyDamageExpect(_attackCharacter, _targetCharacter);

                if (Input.GetKeyUp(KeyCode.Space))
                {
                    // �I�������L�����N�^�[���G�ł���ꍇ�͍U���J�n
                    if( _targetCharacter != null && _targetCharacter.param.characterTag == Character.CHARACTER_TAG.CHARACTER_ENEMY )
                    {
                        // �I���O���b�h���ꎞ��\��
                        BattleUISystem.Instance.ToggleSelectGrid(false);
                        // �U���V�[�P���X��������
                        _attackSequence.Init(_attackCharacter, _targetCharacter);
                        // �A�^�b�N�J�[�\��UI��\��
                        btlUIInstance.ToggleAttackCursorP2E(false);
                        // �_���[�W�\���\��UI���\��
                        btlUIInstance.ToggleBattleExpect(false);
                        // �O���b�h��Ԃ̕`����N���A
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

        //���S�����ʒm(����̃J�E���^�[�ɂ���ē|�����\�������邽�߁A��������)
        Character diedCharacter = _attackSequence.GetDiedCharacter();
        if (diedCharacter != null)
        {
            var key = new CharacterHashtable.Key(diedCharacter.param.characterTag, diedCharacter.param.characterIndex);
            NoticeCharacterDied(key);
            // �j��
            diedCharacter.Remove();
        }

        // �A�^�b�J�[�L�����N�^�[�̐ݒ������
        btlInstance.ResetAttackerCharacter();
        // �\���_���[�W�����Z�b�g
        btlInstance.ResetDamageExpect(_attackCharacter, _targetCharacter);
        // �A�^�b�N�J�[�\��UI��\��
        btlUIInstance.ToggleAttackCursorP2E(false);
        // �_���[�W�\���\��UI���\��
        btlUIInstance.ToggleBattleExpect(false);
        // �g�p�X�L���̓_�ł��\��
        for (int i = 0; i < Constants.EQUIPABLE_SKILL_MAX_NUM; ++i) {
            btlUIInstance.PlayerParameter.GetSkillBox(i).SetFlickEnabled(false);
            btlUIInstance.PlayerParameter.GetSkillBox(i).SetUseable(true);
            btlUIInstance.EnemyParameter.GetSkillBox(i).SetFlickEnabled(false);
            btlUIInstance.EnemyParameter.GetSkillBox(i).SetUseable(true);
        }
        // �g�p�X�L���R�X�g���ς�������Z�b�g
        _attackCharacter.param.ResetConsumptionActionGauge();
        _targetCharacter.param.ResetConsumptionActionGauge();
        // �O���b�h��Ԃ̕`����N���A
        stgInstance.UpdateGridInfo();
        stgInstance.ClearGridMeshDraw();
        // �I���O���b�h��\��
        BattleUISystem.Instance.ToggleSelectGrid(true);

        base.Exit();
    }
}
