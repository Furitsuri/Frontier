using Frontier.Stage;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Frontier.SkillsData;

namespace Frontier
{
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
            var stgInstance = Stage.StageController.Instance;
            var btlUIInstance = BattleUISystem.Instance;

            base.Init();

            _phase = PLAttackPhase.PL_ATTACK_SELECT_GRID;
            _curentGridIndex = stgInstance.GetCurrentGridIndex();

            // ���ݑI�𒆂̃L�����N�^�[�����擾���čU���͈͂�\��
            _attackCharacter = _btlMgr.GetCharacterFromHashtable(_btlMgr.SelectCharacterInfo);
            if (_attackCharacter == null)
            {
                Debug.Assert(false, "SelectPlayer Irregular.");
                return;
            }
            var param = _attackCharacter.param;
            stgInstance.RegistAttackAbleInfo(_curentGridIndex, param.attackRange, param.characterTag);
            stgInstance.DrawAttackableGrids(_curentGridIndex);

            // �U���\�ȃO���b�h���ɓG�������ꍇ�ɕW�I�O���b�h�����킹��
            if (stgInstance.RegistAttackTargetGridIndexs(Character.CHARACTER_TAG.ENEMY))
            {
                // �A�^�b�J�[�L�����N�^�[�̐ݒ�
                stgInstance.BindGridCursorState(GridCursor.State.ATTACK, _attackCharacter);
                // �A�^�b�N�J�[�\��UI�\��
                btlUIInstance.ToggleAttackCursorP2E(true);
            }
        }

        public override bool Update()
        {
            var stgInstance = Stage.StageController.Instance;
            var btlUIInstance = BattleUISystem.Instance;

            if (base.Update())
            {
                return true;
            }

            // �U���\��ԂłȂ���Ή������Ȃ�
            if (stgInstance.GetGridCursorState() != Stage.GridCursor.State.ATTACK)
            {
                return false;
            }

            switch (_phase)
            {
                case PLAttackPhase.PL_ATTACK_SELECT_GRID:
                    // �O���b�h�̑���
                    Stage.StageController.Instance.OperateGridCursor();
                    // �O���b�h��̃L�����N�^�[���擾
                    _targetCharacter = _btlMgr.GetSelectCharacter();
                    // �_���[�W�\���\��UI��\��
                    btlUIInstance.ToggleBattleExpect(true);
                    // �g�p�X�L����I������
                    _attackCharacter.SelectUseSkills(SituationType.ATTACK);
                    _targetCharacter.SelectUseSkills(SituationType.DEFENCE);

                    // �\���_���[�W��K������
                    _btlMgr.ApplyDamageExpect(_attackCharacter, _targetCharacter);

                    if (Input.GetKeyUp(KeyCode.Space))
                    {
                        // �I�������L�����N�^�[���G�ł���ꍇ�͍U���J�n
                        if (_targetCharacter != null && _targetCharacter.param.characterTag == Character.CHARACTER_TAG.ENEMY)
                        {
                            // �L�����N�^�[�̃A�N�V�����Q�[�W������
                            _attackCharacter.ConsumeActionGauge();
                            _targetCharacter.ConsumeActionGauge();
                            // �I���O���b�h���ꎞ��\��
                            stgInstance.SetGridCursorActive(false);
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
                    if (_attackSequence.Update())
                    {
                        _phase = PLAttackPhase.PL_ATTACK_END;
                    }

                    break;
                case PLAttackPhase.PL_ATTACK_END:
                    // �U�������L�����N�^�[�̍U���R�}���h��I��s�ɂ���
                    _attackCharacter.tmpParam.isEndCommand[(int)Character.Command.COMMAND_TAG.ATTACK] = true;
                    // �R�}���h�I���ɖ߂�
                    Back();

                    return true;
            }

            return false;
        }

        public override void Exit()
        {
            var btlUIInstance = BattleUISystem.Instance;
            var stgInstance = Stage.StageController.Instance;

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
            stgInstance.ClearGridCursroBind();
            // �\���_���[�W�����Z�b�g
            _btlMgr.ResetDamageExpect(_attackCharacter, _targetCharacter);
            // �A�^�b�N�J�[�\��UI��\��
            btlUIInstance.ToggleAttackCursorP2E(false);
            // �_���[�W�\���\��UI���\��
            btlUIInstance.ToggleBattleExpect(false);
            // �g�p�X�L���̓_�ł��\��
            for (int i = 0; i < Constants.EQUIPABLE_SKILL_MAX_NUM; ++i)
            {
                btlUIInstance.GetPlayerParamSkillBox(i).SetFlickEnabled(false);
                btlUIInstance.GetPlayerParamSkillBox(i).SetUseable(true);
                btlUIInstance.GetEnemyParamSkillBox(i).SetFlickEnabled(false);
                btlUIInstance.GetEnemyParamSkillBox(i).SetUseable(true);
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
            stgInstance.SetGridCursorActive(true);

            base.Exit();
        }
    }
}