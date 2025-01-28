using Frontier.Stage;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier
{
    public class EMAttackState : PhaseStateBase
    {
        private enum EMAttackPhase
        {
            EM_ATTACK_CONFIRM = 0,
            EM_ATTACK_EXECUTE,
            EM_ATTACK_END,
        }

        private EMAttackPhase _phase;
        private int _curentGridIndex                    = -1;
        private Enemy _attackCharacter                  = null;
        private Character _targetCharacter              = null;
        private CharacterAttackSequence _attackSequence = null;

        public override void Init()
        {
            var btlUIInstance = BattleUISystem.Instance;

            base.Init();

            _attackSequence = _hierarchyBld.InstantiateWithDiContainer<CharacterAttackSequence>();
            _curentGridIndex = _stageCtrl.GetCurrentGridIndex();
            _attackCharacter = _btlMgr.BtlCharaCdr.GetSelectCharacter() as Enemy;
            Debug.Assert(_attackCharacter != null);

            // ���ݑI�𒆂̃L�����N�^�[�����擾���čU���͈͂�\��
            var param = _attackCharacter.param;
            _stageCtrl.RegistAttackAbleInfo(_curentGridIndex, param.attackRange, param.characterTag);
            _stageCtrl.DrawAttackableGrids(_curentGridIndex);

            // �U���\�ȃO���b�h���ɓG�������ꍇ�ɕW�I�O���b�h�����킹��
            if (_stageCtrl.RegistAttackTargetGridIndexs(Character.CHARACTER_TAG.PLAYER, _attackCharacter.GetAi().GetTargetCharacter()))
            {
                // �A�^�b�J�[�L�����N�^�[�̐ݒ�
                _stageCtrl.BindGridCursorState(GridCursor.State.ATTACK, _attackCharacter);
                // �A�^�b�N�J�[�\��UI�\��
                btlUIInstance.ToggleAttackCursorE2P(true);
            }

            _targetCharacter = _attackCharacter.GetAi().GetTargetCharacter();
            _stageCtrl.ApplyCurrentGrid2CharacterGrid(_attackCharacter);

            // �U���҂̌�����ݒ�
            var targetGridInfo = _stageCtrl.GetGridInfo(_targetCharacter.tmpParam.gridIndex);
            _attackCharacter.RotateToPosition(targetGridInfo.charaStandPos);
            var attackerGridInfo = _stageCtrl.GetGridInfo(_attackCharacter.tmpParam.gridIndex);
            _targetCharacter.RotateToPosition(attackerGridInfo.charaStandPos);

            // �U���V�[�P���X��������
            _attackSequence.Init();

            _phase = EMAttackPhase.EM_ATTACK_CONFIRM;
        }

        public override bool Update()
        {
            var btlUIInstance = BattleUISystem.Instance;

            // �U���\��ԂłȂ���Ή������Ȃ�
            if (_stageCtrl.GetGridCursorState() != GridCursor.State.ATTACK)
            {
                return false;
            }

            switch (_phase)
            {
                case EMAttackPhase.EM_ATTACK_CONFIRM:
                    // �g�p�X�L����I������
                    _attackCharacter.SelectUseSkills(SkillsData.SituationType.ATTACK);
                    _targetCharacter.SelectUseSkills(SkillsData.SituationType.DEFENCE);

                    // �\���_���[�W��K������
                    _btlMgr.BtlCharaCdr.ApplyDamageExpect(_attackCharacter, _targetCharacter);

                    // �_���[�W�\���\��UI��\��
                    btlUIInstance.ToggleBattleExpect(true);

                    if (Input.GetKeyUp(KeyCode.Space))
                    {
                        // �L�����N�^�[�̃A�N�V�����Q�[�W������
                        _attackCharacter.ConsumeActionGauge();
                        _targetCharacter.ConsumeActionGauge();

                        // �I���O���b�h���ꎞ��\��
                        _stageCtrl.SetGridCursorActive(false);

                        // �A�^�b�N�J�[�\��UI��\��
                        btlUIInstance.ToggleAttackCursorE2P(false);

                        // �_���[�W�\���\��UI���\��
                        btlUIInstance.ToggleBattleExpect(false);

                        // �O���b�h��Ԃ̕`����N���A
                        _stageCtrl.ClearGridMeshDraw();

                        // �U���V�[�P���X�̊J�n
                        _attackSequence.StartSequence(_attackCharacter, _targetCharacter);

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

            //���S�����ʒm(����̃J�E���^�[�ɂ���ē|�����\�������邽�߁A�U���҂Ɣ�U���҂̗����𔻒�)
            Character diedCharacter = _attackSequence.GetDiedCharacter();
            if (diedCharacter != null)
            {
                var key = new CharacterHashtable.Key(diedCharacter.param.characterTag, diedCharacter.param.characterIndex);
                NoticeCharacterDied(key);
                // �j��
                diedCharacter.Remove();
            }

            // �A�^�b�J�[�L�����N�^�[�̐ݒ������
            _stageCtrl.ClearGridCursroBind();
            // �\���_���[�W�����Z�b�g
            _btlMgr.BtlCharaCdr.ResetDamageExpect(_attackCharacter, _targetCharacter);

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
            _stageCtrl.UpdateGridInfo();
            _stageCtrl.ClearGridMeshDraw();
            // �I���O���b�h��\��
            // �����̍U���̒���Ƀv���C���[�t�F�[�Y�Ɉڍs�����ꍇ�A��u�̊ԁA�I���O���b�h���\������A
            //   ���̌�v���C���[�ɑI���O���b�h���ڂ�Ƃ����󋵂ɂȂ�܂��B
            //   ���̋����������o�O�̂悤�Ɍ����Ă��܂��̂ŁA���������܂܂ɂ��邱�Ƃɂ��A
            //   ���̃L�����N�^�[���s���J�n����ۂɕ\������悤�ɂ��܂��B
            // Stage.StageController.Instance.SetGridCursorActive(true);

            base.Exit();
        }
    }
}