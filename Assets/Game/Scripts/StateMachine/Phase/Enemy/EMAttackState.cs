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

        // ���ݑI�𒆂̃L�����N�^�[�����擾���čU���͈͂�\��
        var param = _enemy.param;
        stgInstance.RegistAttackAbleInfo(_curentGridIndex, param.attackRange, param.characterTag);
        stgInstance.DrawAttackableGrids(_curentGridIndex);

        // �U���\�ȃO���b�h���ɓG�������ꍇ�ɕW�I�O���b�h�����킹��
        if (stgInstance.RegistAttackTargetGridIndexs(Character.CHARACTER_TAG.CHARACTER_PLAYER))
        {
            // �A�^�b�J�[�L�����N�^�[�̐ݒ�
            btlInstance.SetAttackerCharacter(_enemy);
            // �A�^�b�N�J�[�\��UI�\��
            btlUIInstance.ToggleAttackCursorE2P(true);
        }

        _targetCharacter = _enemy.EmAI.GetTargetCharacter();
        stgInstance.ApplyCurrentGrid2CharacterGrid(_enemy);
        // �\���_���[�W��K������
        btlInstance.ApplyDamageExpect(_enemy, _targetCharacter);
        // �_���[�W�\���\��UI��\��
        btlUIInstance.ToggleBattleExpect(true);

        _phase = EMAttackPhase.EM_ATTACK_CONFIRM;
    }

    public override bool Update()
    {
        var stgInstance = StageGrid.Instance;
        var btlInstance = BattleManager.Instance;
        var btlUIInstance = BattleUISystem.Instance;

        // �U���\��ԂłȂ���Ή������Ȃ�
        if (!btlInstance.IsAttackPhaseState())
        {
            return false;
        }

        switch (_phase)
        {
            case EMAttackPhase.EM_ATTACK_CONFIRM:
                if(Input.GetKeyUp(KeyCode.Space))
                {
                    // �I���O���b�h���ꎞ��\��
                    BattleUISystem.Instance.ToggleSelectGrid(false);
                    // �U���V�[�P���X��������
                    _attackSequence.Init(_enemy, _targetCharacter);
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
                _enemy.tmpParam.isEndCommand[(int)Character.BaseCommand.COMMAND_ATTACK] = true;
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
        btlInstance.ResetDamageExpect(_enemy, _targetCharacter);
        // �A�^�b�N�J�[�\��UI��\��
        btlUIInstance.ToggleAttackCursorP2E(false);
        // �_���[�W�\���\��UI���\��
        btlUIInstance.ToggleBattleExpect(false);
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
