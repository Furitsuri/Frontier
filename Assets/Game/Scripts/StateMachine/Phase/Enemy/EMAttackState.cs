using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SkillsData;

public class EMAttackState : PhaseStateBase
{
    private enum EMAttackPhase
    {
        EM_ATTACK_CONFIRM = 0,
        EM_ATTACK_EXECUTE,
        EM_ATTACK_END,
    }

    private EMAttackPhase _phase;
    private Enemy _attackCharacter = null;
    private Character _targetCharacter = null;
    private int _curentGridIndex = -1;
    private CharacterAttackSequence _attackSequence = new CharacterAttackSequence();

    public override void Init()
    {
        var stgInstance     = StageGrid.Instance;
        var btlInstance     = BattleManager.Instance;
        var btlUIInstance   = BattleUISystem.Instance;

        base.Init();

        _curentGridIndex    = stgInstance.GetCurrentGridIndex();
        _attackCharacter  = btlInstance.GetSelectCharacter() as Enemy;
        Debug.Assert(_attackCharacter != null);

        // ���ݑI�𒆂̃L�����N�^�[�����擾���čU���͈͂�\��
        var param = _attackCharacter.param;
        stgInstance.RegistAttackAbleInfo(_curentGridIndex, param.attackRange, param.characterTag);
        stgInstance.DrawAttackableGrids(_curentGridIndex);

        // �U���\�ȃO���b�h���ɓG�������ꍇ�ɕW�I�O���b�h�����킹��
        if (stgInstance.RegistAttackTargetGridIndexs(Character.CHARACTER_TAG.CHARACTER_PLAYER, _attackCharacter.EmAI.GetTargetCharacter()))
        {
            // �A�^�b�J�[�L�����N�^�[�̐ݒ�
            btlInstance.SetAttackerCharacter(_attackCharacter);
            // �A�^�b�N�J�[�\��UI�\��
            btlUIInstance.ToggleAttackCursorE2P(true);
        }

        _targetCharacter = _attackCharacter.EmAI.GetTargetCharacter();
        stgInstance.ApplyCurrentGrid2CharacterGrid(_attackCharacter);
        // �\���_���[�W��K������
        btlInstance.ApplyDamageExpect(_attackCharacter, _targetCharacter);
        // �_���[�W�\���\��UI��\��
        btlUIInstance.ToggleBattleExpect(true);

        _phase = EMAttackPhase.EM_ATTACK_CONFIRM;
    }

    public override bool Update()
    {
        var stgInstance     = StageGrid.Instance;
        var btlInstance     = BattleManager.Instance;
        var btlUIInstance   = BattleUISystem.Instance;

        // �U���\��ԂłȂ���Ή������Ȃ�
        if (!btlInstance.IsAttackPhaseState())
        {
            return false;
        }

        switch (_phase)
        {
            case EMAttackPhase.EM_ATTACK_CONFIRM:
                // �g�p�X�L����I������
                _attackCharacter.SelectUseSkills(SituationType.ATTACK);
                _targetCharacter.SelectUseSkills(SituationType.DEFENCE);

                if (Input.GetKeyUp(KeyCode.Space))
                {
                    // �L�����N�^�[�̃A�N�V�����Q�[�W������
                    _attackCharacter.ConsumeActionGauge();
                    _targetCharacter.ConsumeActionGauge();
                    // �I���O���b�h���ꎞ��\��
                    BattleUISystem.Instance.ToggleSelectGrid(false);
                    // �U���V�[�P���X��������
                    _attackSequence.Init(_attackCharacter, _targetCharacter);
                    // �A�^�b�N�J�[�\��UI��\��
                    btlUIInstance.ToggleAttackCursorE2P(false);
                    // �_���[�W�\���\��UI���\��
                    btlUIInstance.ToggleBattleExpect(false);
                    // �O���b�h��Ԃ̕`����N���A
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
        for (int i = 0; i < Constants.EQUIPABLE_SKILL_MAX_NUM; ++i)
        {
            btlUIInstance.PlayerParameter.GetSkillBox(i).SetFlickEnabled(false);
            btlUIInstance.PlayerParameter.GetSkillBox(i).SetUseable(true);
            btlUIInstance.EnemyParameter.GetSkillBox(i).SetFlickEnabled(false);
            btlUIInstance.EnemyParameter.GetSkillBox(i).SetUseable(true);
        }
        // �g�p�X�L���R�X�g���ς�������Z�b�g
        _attackCharacter.param.ResetConsumptionActionGauge();
        _attackCharacter.skillModifiedParam.Reset();
        _targetCharacter.param.ResetConsumptionActionGauge();
        _targetCharacter.skillModifiedParam.Reset();
        // �O���b�h��Ԃ̕`����N���A
        stgInstance.UpdateGridInfo();
        stgInstance.ClearGridMeshDraw();
        // �I���O���b�h��\��
        // �����̍U���̒���Ƀv���C���[�t�F�[�Y�Ɉڍs�����ꍇ�A��u�̊ԁA�I���O���b�h���\������A
        //   ���̌�v���C���[�ɑI���O���b�h���ڂ�Ƃ����󋵂ɂȂ�܂��B
        //   ���̋����������o�O�̂悤�Ɍ����Ă��܂��̂ŁA���������܂܂ɂ��邱�Ƃɂ��A
        //   ���̃L�����N�^�[���s���J�n����ۂɕ\������悤�ɂ��܂��B
        // BattleUISystem.Instance.ToggleSelectGrid(true);

        base.Exit();
    }
}
