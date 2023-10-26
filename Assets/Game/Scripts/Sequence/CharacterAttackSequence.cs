using Frontier.Stage;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TMPro.Examples;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace Frontier
{
    public class CharacterAttackSequence
    {
        enum Phase
        {
            START,
            WAIT_ATTACK,
            ATTACK,
            PARRY,
            COUNTER,
            DIE,
            WAIT_END,
            END
        }

        delegate bool UpdateAttack(in Vector3 arg1, in Vector3 arg2);

        private Phase _phase;
        private float _elapsedTime = 0f;
        private bool _CounterConditions = false;
        private BattleManager _btlMgr = null;
        private BattleCameraController _btlCamCtrl = null;
        private StageController _stageCtrl = null;
        private Character _attackCharacter = null;
        private Character _targetCharacter = null;
        private Character _diedCharacter = null;
        // Transform�͒x�����߃L���b�V��
        private Transform _atkCharaTransform = null;
        private Transform _tgtCharaTransform = null;
        private Vector3 _departure = Vector3.zero;
        private Vector3 _destination = Vector3.zero;
        private Quaternion _atkCharaInitialRot = Quaternion.identity;
        private Quaternion _tgtCharaInitialRot = Quaternion.identity;
        private UpdateAttack _updateAttackerAttack = null;
        private UpdateAttack _updateTargetAttack = null;

        public void Init(Character attackChara, Character targetChara)
        {
            _btlMgr = ManagerProvider.Instance.GetService<BattleManager>();
            _btlCamCtrl = _btlMgr.GetCameraController();
            _stageCtrl = ManagerProvider.Instance.GetService<StageController>();
            _attackCharacter = attackChara;
            _targetCharacter = targetChara;
            _diedCharacter = null;
            _atkCharaTransform = _attackCharacter.transform;
            _tgtCharaTransform = _targetCharacter.transform;
            _atkCharaInitialRot = _atkCharaTransform.rotation;
            _tgtCharaInitialRot = _tgtCharaTransform.rotation;
            _elapsedTime = 0f;
            _phase = Phase.START;
            // �ΐ푊��Ƃ��Đݒ�
            _attackCharacter.SetOpponentCharacter(_targetCharacter);
            _targetCharacter.SetOpponentCharacter(_attackCharacter);
            // �J�E���^�[�����̐ݒ�
            _CounterConditions = (_targetCharacter.IsSkillInUse(SkillsData.ID.SKILL_COUNTER));
            // �U���X�V�����̏����ʐݒ�
            if (_CounterConditions && _attackCharacter.GetBullet() != null) _CounterConditions = _targetCharacter.GetBullet() != null;
            if (_attackCharacter.GetBullet() == null) _updateAttackerAttack = _attackCharacter.UpdateClosedAttack;
            else _updateAttackerAttack = _attackCharacter.UpdateRangedAttack;
            if (_targetCharacter.GetBullet() == null) _updateTargetAttack = _targetCharacter.UpdateClosedAttack;
            else _updateTargetAttack = _targetCharacter.UpdateRangedAttack;

            _btlCamCtrl.StartAttackSequenceMode(attackChara, targetChara);
        }

        // Update is called once per frame
        public bool Update()
        {
            switch (_phase)
            {
                case Phase.START:
                    // START_ROTATION_TIME���o�߂���܂Ō�����ύX���܂�
                    _elapsedTime += Time.deltaTime;
                    float t = Mathf.Clamp01(_elapsedTime / Constants.ATTACK_ROTATIION_TIME);
                    t = Mathf.SmoothStep(0f, 1f, t);

                    Quaternion destAttackerRot = Quaternion.LookRotation(_tgtCharaTransform.position - _atkCharaTransform.position);
                    Quaternion destTargetRot = Quaternion.LookRotation(_atkCharaTransform.position - _tgtCharaTransform.position);
                    _atkCharaTransform.rotation = Quaternion.Lerp(_atkCharaInitialRot, destAttackerRot, t);
                    _tgtCharaTransform.rotation = Quaternion.Lerp(_tgtCharaInitialRot, destTargetRot, t);

                    if (_btlCamCtrl.IsFadeAttack())
                    {
                        _elapsedTime = 0f;

                        TransitBattleField(_attackCharacter, _targetCharacter);

                        _phase = Phase.WAIT_ATTACK;
                    }
                    break;
                case Phase.WAIT_ATTACK:
                    if (Constants.ATTACK_SEQUENCE_WAIT_ATTACK_TIME < (_elapsedTime += Time.deltaTime))
                    {
                        _elapsedTime = 0f;
                        StartAttack(_attackCharacter, _targetCharacter);

                        // �p���B�X�L���g�p���̓p���B�����p�����֑J��
                        if (_targetCharacter.IsSkillInUse(SkillsData.ID.SKILL_PARRY)) _phase = Phase.PARRY;
                        else
                        {
                            _phase = Phase.ATTACK;
                        }
                    }
                    break;
                case Phase.ATTACK:
                    if (_updateAttackerAttack(_departure, _destination))
                    {
                        // �J�����ΏۂƃJ�����p�����[�^��ύX
                        _btlCamCtrl.TransitNextPhaseCameraParam(null, _targetCharacter.transform);
                        // �K�[�h�X�L�����g�p���̓K�[�h���[�V������߂�
                        if (_targetCharacter.IsSkillInUse(SkillsData.ID.SKILL_GUARD)) _targetCharacter.setAnimator(Character.ANIME_TAG.GUARD, false);
                        // �Ώۂ����S���Ă���ꍇ�͎��S������
                        if (_targetCharacter.IsDead())
                        {
                            _diedCharacter = _targetCharacter;
                            _phase = Phase.DIE;
                        }
                        // �J�E���^�[�X�L�����o�^����Ă���ꍇ�̓J�E���^�[������
                        else if (_CounterConditions)
                        {
                            _btlMgr.ApplyDamageExpect(_targetCharacter, _attackCharacter);
                            StartAttack(_targetCharacter, _attackCharacter);

                            _phase = Phase.COUNTER;
                        }
                        else _phase = Phase.WAIT_END;
                    }
                    break;
                case Phase.PARRY:
                    break;
                case Phase.COUNTER:
                    if (_updateTargetAttack(_departure, _destination))
                    {
                        // �J�����ΏۂƃJ�����p�����[�^��ύX
                        _btlCamCtrl.TransitNextPhaseCameraParam(null, _targetCharacter.transform);

                        if (_attackCharacter.IsDead())
                        {
                            _diedCharacter = _attackCharacter;
                            _phase = Phase.DIE;
                        }
                        else _phase = Phase.WAIT_END;
                    }
                    break;
                case Phase.DIE:
                    if (_targetCharacter.IsEndAnimation(Character.ANIME_TAG.DIE))
                    {
                        _phase = Phase.WAIT_END;
                    }
                    break;
                case Phase.WAIT_END:
                    if (Constants.ATTACK_SEQUENCE_WAIT_END_TIME < (_elapsedTime += Time.deltaTime))
                    {
                        _elapsedTime = 0f;
                        // �_���[�WUI���\��
                        BattleUISystem.Instance.ToggleDamageUI(false);
                        // �o�g���t�B�[���h����X�e�[�W�t�B�[���h�ɑJ��
                        TransitStageField(_attackCharacter, _targetCharacter);
                        // �U���V�[�P���X�p�J�������I��
                        var info = _stageCtrl.GetGridInfo(_attackCharacter.tmpParam.gridIndex);
                        _btlCamCtrl.EndAttackSequenceMode(_attackCharacter);

                        _phase = Phase.END;
                    }
                    break;
                case Phase.END:
                    if (_btlCamCtrl.IsFadeEnd())
                    {
                        // �ΐ푊��ݒ�����Z�b�g
                        _attackCharacter.ResetOpponentCharacter();
                        _targetCharacter.ResetOpponentCharacter();

                        return true;
                    }
                    break;
            }

            return false;
        }

        /// <summary>
        /// ���S�L�����N�^�[��Ԃ��܂�
        /// </summary>
        /// <returns>���S�L�����N�^�[</returns>
        public Character GetDiedCharacter() { return _diedCharacter; }


        /// <summary>
        /// �U���L�����Ɣ�U���L�����ԂƂ̍U�����������s���܂�
        /// </summary>
        /// <param name="attacker">�U���L�����N�^�[</param>
        /// <param name="target">��U���L�����N�^�[</param>
        private void StartAttack(Character attacker, Character target)
        {
            if (attacker.GetBullet() != null) attacker.StartRangedAttack();
            else
            {
                _departure = attacker.transform.position;
                _destination = target.transform.position + target.transform.forward;
                attacker.StartClosedAttack();
            }

            // �^�[�Q�b�g���K�[�h�X�L���g�p���̓K�[�h���[�V�������Đ�
            if (target.IsSkillInUse(SkillsData.ID.SKILL_GUARD)) target.setAnimator(Character.ANIME_TAG.GUARD, true);
        }

        /// <summary>
        /// �퓬�t�B�[���h�ɑJ�ڂ��܂�
        /// </summary>
        /// <param name="attacker">�U���L�����N�^�[</param>
        /// <param name="target">��U���L�����N�^�[</param>
        private void TransitBattleField(Character attacker, Character target)
        {
            // ���b�V���y��attaker��target�ȊO�̃L�����N�^�[���\����
            _stageCtrl.ToggleMeshDisplay(false);
            foreach (var player in _btlMgr.GetPlayerEnumerable())
            {
                if (player != attacker && player != target)
                {
                    player.gameObject.SetActive(false);
                }
            }
            foreach (var enemy in _btlMgr.GetEnemyEnumerable())
            {
                if (enemy != attacker && enemy != target)
                {
                    enemy.gameObject.SetActive(false);
                }
            }

            // �L�����N�^�[���X�e�[�W�̒��S�ʒu���炻�ꂼ�ꗣ�ꂽ�ꏊ�ɗ�������
            var centralPos = _stageCtrl.transform.position;

            // �����ƓG�Α��ŕ���
            Character ally = null;
            Character opponent = null;
            if (attacker.IsPlayer())
            {
                ally = attacker;
                opponent = target;
            }
            else
            {
                if (target.IsPlayer())
                {
                    ally = target;
                    opponent = attacker;
                }
                else if (target.IsOther())
                {
                    ally = target;
                    opponent = attacker;
                }
                else
                {
                    ally = attacker;
                    opponent = target;
                }
            }

            // �����͉��s��O���A�G�͉��s�����̗����ʒu�Ƃ���
            Transform allyTransform = ally.transform;
            Transform opponentTransform = opponent.transform;
            allyTransform.position = centralPos + new Vector3(0f, 0f, -_stageCtrl.BattlePosLengthFromCentral);
            opponentTransform.position = centralPos + new Vector3(0f, 0f, _stageCtrl.BattlePosLengthFromCentral);
            allyTransform.rotation = Quaternion.LookRotation(centralPos - allyTransform.position);
            opponentTransform.rotation = Quaternion.LookRotation(centralPos - opponentTransform.position);
            // �퓬�t�B�[���h�̃J�����ݒ�ɓK��������
            _btlCamCtrl.AdaptBattleFieldSetting(attacker, target);
        }

        /// <summary>
        /// �X�e�[�W�t�B�[���h�ɑJ�ڂ��܂�
        /// </summary>
        /// <param name="attacker">�U���L�����N�^�[</param>
        /// <param name="target">��U���L�����N�^�[</param>
        private void TransitStageField(Character attacker, Character target)
        {
            // ��\���ɂ��Ă������̂�\��
            _stageCtrl.ToggleMeshDisplay(true);
            foreach (var player in _btlMgr.GetPlayerEnumerable())
            {
                player.gameObject.SetActive(true);
            }
            foreach (var enemy in _btlMgr.GetEnemyEnumerable())
            {
                enemy.gameObject.SetActive(true);
            }

            // �L�����N�^�[���X�e�[�W�̒��S�ʒu���炻�ꂼ�ꗣ�ꂽ�ꏊ�ɗ�������
            var info = _stageCtrl.GetGridInfo(attacker.tmpParam.gridIndex);
            _attackCharacter.transform.position = info.charaStandPos;
            _attackCharacter.transform.rotation = _atkCharaInitialRot;
            info = _stageCtrl.GetGridInfo(target.tmpParam.gridIndex);
            _targetCharacter.transform.position = info.charaStandPos;
            _targetCharacter.transform.rotation = _tgtCharaInitialRot;
        }
    }
}