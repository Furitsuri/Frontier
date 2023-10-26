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
        private int _curentGridIndex = -1;
        private Character _attackCharacter = null;
        private Character _targetCharacter = null;
        private CharacterAttackSequence _attackSequence = new CharacterAttackSequence();

        override public void Init()
        {
            var btlUIInstance = BattleUISystem.Instance;

            base.Init();

            _phase = PLAttackPhase.PL_ATTACK_SELECT_GRID;
            _curentGridIndex = _stageCtrl.GetCurrentGridIndex();

            // ���ݑI�𒆂̃L�����N�^�[�����擾���čU���͈͂�\��
            _attackCharacter = _btlMgr.GetCharacterFromHashtable(_btlMgr.SelectCharacterInfo);
            if (_attackCharacter == null)
            {
                Debug.Assert(false, "SelectPlayer Irregular.");
                return;
            }
            var param = _attackCharacter.param;
            _stageCtrl.RegistAttackAbleInfo(_curentGridIndex, param.attackRange, param.characterTag);
            _stageCtrl.DrawAttackableGrids(_curentGridIndex);

            // �U���\�ȃO���b�h���ɓG�������ꍇ�ɕW�I�O���b�h�����킹��
            if (_stageCtrl.RegistAttackTargetGridIndexs(Character.CHARACTER_TAG.ENEMY))
            {
                // �A�^�b�J�[�L�����N�^�[�̐ݒ�
                _stageCtrl.BindGridCursorState(GridCursor.State.ATTACK, _attackCharacter);
                // �A�^�b�N�J�[�\��UI�\��
                btlUIInstance.ToggleAttackCursorP2E(true);
                // �U���҂̌������X�V
                GridInfo info;
                _stageCtrl.FetchCurrentGridInfo(out info);
                _attackCharacter.OrderRotateToPosition(info.charaStandPos);
            }
        }

        public override bool Update()
        {
            var btlUIInstance = BattleUISystem.Instance;

            if (base.Update())
            {
                return true;
            }

            // �U���\��ԂłȂ���Ή������Ȃ�
            if (_stageCtrl.GetGridCursorState() != Stage.GridCursor.State.ATTACK)
            {
                return false;
            }

            switch (_phase)
            {
                case PLAttackPhase.PL_ATTACK_SELECT_GRID:
                    // �O���b�h�̑���
                    _stageCtrl.OperateGridCursor();
                    // �O���b�h��̃L�����N�^�[���擾
                    var prevTargetCharacter = _targetCharacter;
                    _targetCharacter = _btlMgr.GetSelectCharacter();
                    // �I���L�����N�^�[���X�V���ꂽ�ꍇ�͌������X�V
                    if( prevTargetCharacter != _targetCharacter )
                    {
                        var info = _stageCtrl.GetGridInfo(_targetCharacter.tmpParam.gridIndex);
                        _attackCharacter.OrderRotateToPosition( info.charaStandPos );
                    }
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
                            _stageCtrl.SetGridCursorActive(false);
                            // �U���V�[�P���X��������
                            _attackSequence.Init(_attackCharacter, _targetCharacter);
                            // �A�^�b�N�J�[�\��UI��\��
                            btlUIInstance.ToggleAttackCursorP2E(false);
                            // �_���[�W�\���\��UI���\��
                            btlUIInstance.ToggleBattleExpect(false);
                            // �O���b�h��Ԃ̕`����N���A
                            _stageCtrl.ClearGridMeshDraw();

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
            _stageCtrl.UpdateGridInfo();
            _stageCtrl.ClearGridMeshDraw();
            // �I���O���b�h��\��
            _stageCtrl.SetGridCursorActive(true);

            base.Exit();
        }
    }
}