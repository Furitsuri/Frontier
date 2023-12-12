using Frontier.Stage;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using TMPro.Examples;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static Frontier.Character;
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
            WAIT_PARRY_START,
            WAIT_PARRY_END,
            EXEC_PARRY,
            COUNTER,
            DIE,
            WAIT_END,
            END
        }

        delegate bool UpdateAttack(in Vector3 arg1, in Vector3 arg2);

        private Phase _phase;
        private float _elapsedTime = 0f;
        private bool _counterConditions = false;
        private bool _parryConditions = false;
        private bool _isJustParry = false;
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

        /// <summary>
        /// ���������܂�
        /// </summary>
        /// <param name="btlMgr">�o�g���}�l�[�W��</param>
        /// <param name="stgCtrl">�X�e�[�W�R���g���[��</param>
        /// <param name="attackChara">�U���L�����N�^�[</param>
        /// <param name="targetChara">��U���L�����N�^�[</param>
        public void Init(BattleManager btlMgr, StageController stgCtrl, Character attackChara, Character targetChara)
        {
            _btlMgr = btlMgr;
            _btlCamCtrl = _btlMgr.GetCameraController();
            _stageCtrl = stgCtrl;
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
            _counterConditions = _targetCharacter.IsSkillInUse(SkillsData.ID.SKILL_COUNTER);
            // �p���B�����̐ݒ�
            _parryConditions = _targetCharacter.IsSkillInUse(SkillsData.ID.SKILL_PARRY);
            // �U���X�V�����̏����ʐݒ�
            if (_counterConditions && _attackCharacter.GetBullet() != null) _counterConditions = _targetCharacter.GetBullet() != null;
            if (_attackCharacter.GetBullet() == null) _updateAttackerAttack = _attackCharacter.UpdateClosedAttack;
            else _updateAttackerAttack = _attackCharacter.UpdateRangedAttack;
            if (_targetCharacter.GetBullet() == null) _updateTargetAttack =  _targetCharacter.UpdateClosedAttack;
            else _updateTargetAttack = _targetCharacter.UpdateRangedAttack;

            _btlCamCtrl.StartAttackSequenceMode(attackChara, targetChara);
        }

        // Update is called once per frame
        public bool Update()
        {
            var parryCtrl = _btlMgr.SkillCtrl.ParryController;

            switch (_phase)
            {
                case Phase.START:
                    // START_ROTATION_TIME���o�߂���܂Ō�����ύX���܂�
                    _elapsedTime += Time.deltaTime;
                    float t = Mathf.Clamp01(_elapsedTime / Constants.ATTACK_ROTATIION_TIME);
                    t = Mathf.SmoothStep(0f, 1f, t);

                    Quaternion destAttackerRot  = Quaternion.LookRotation(_tgtCharaTransform.position - _atkCharaTransform.position);
                    Quaternion destTargetRot    = Quaternion.LookRotation(_atkCharaTransform.position - _tgtCharaTransform.position);
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
                        if (_targetCharacter.IsSkillInUse(SkillsData.ID.SKILL_PARRY)) _phase = Phase.WAIT_PARRY_START;
                        // ����ȊO�͒ʏ�ʂ�U����
                        else _phase = Phase.ATTACK;
                    }
                    break;
                case Phase.ATTACK:
                    if (_updateAttackerAttack(_departure, _destination))
                    {
                        // �J�����ΏۂƃJ�����p�����[�^��ύX
                        _btlCamCtrl.TransitNextPhaseCameraParam(null, _targetCharacter.transform);
                        // �K�[�h�X�L�����g�p���̓K�[�h���[�V������߂�
                        if (_targetCharacter.IsSkillInUse(SkillsData.ID.SKILL_GUARD)) _targetCharacter.SetAnimator(AnimDatas.ANIME_CONDITIONS_TAG.GUARD, false);
                        // �Ώۂ����S���Ă���ꍇ�͎��S������
                        if (_targetCharacter.IsDead())
                        {
                            _diedCharacter = _targetCharacter;
                            _phase = Phase.DIE;
                        }
                        // �J�E���^�[�X�L�����o�^����Ă���ꍇ�̓J�E���^�[������
                        else if (_counterConditions)
                        {
                            StartCounter(_attackCharacter, _targetCharacter);

                            _phase = Phase.COUNTER;
                        }
                        else _phase = Phase.WAIT_END;
                    }
                    break;
                case Phase.WAIT_PARRY_START:
                    // �p���B�C�x���g�J�n�܂ōX�V
                    // �U�����L�����N�^�[�̍U�����[�V��������p���B�J�n���\�b�h���Ă΂�邽�߁A
                    // �J�n����Ȃ�(parryCtrl.IsActive��false)�܂܁A�U���X�V���s���邱�Ƃ͑z��O
                    if ( _updateAttackerAttack(_departure, _destination) )
                    {
                        Debug.Assert(false);
                        _phase = Phase.ATTACK;
                    }

                    if ( parryCtrl.IsActive )
                    {
                        _phase = Phase.WAIT_PARRY_END;
                    }
                    break;
                case Phase.WAIT_PARRY_END:
                    if (_updateAttackerAttack(_departure, _destination))
                    {
                        Debug.Assert(false);
                        _phase = Phase.ATTACK;
                    }

                    if ( parryCtrl.IsEndParryEvent )
                    {
                        // �p���B���s�̏ꍇ�͒ʏ�̍U���t�F�[�Y�ֈڍs(���s���̔�_���[�W�{����ParryControler�����p���B���莞�ɏ���)
                        if (parryCtrl.Result == SkillParryController.JudgeResult.FAILED)
                        {
                            _phase = Phase.ATTACK;
                        }
                        else
                        {
                            _isJustParry = (parryCtrl.Result == SkillParryController.JudgeResult.JUST);

                            // �p���B�p�X�V�ɐ؂�ւ��܂�
                            ToggleParryUpdate(_attackCharacter, _targetCharacter);

                            _phase = Phase.EXEC_PARRY;
                        }
                    }
                    break;
                case Phase.EXEC_PARRY:
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
                    if (_targetCharacter.IsEndAnimationOnConditionTag(AnimDatas.ANIME_CONDITIONS_TAG.DIE))
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
                        _attackCharacter.ResetOnEndOfAttackSequence();
                        _targetCharacter.ResetOnEndOfAttackSequence();

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
            if (attacker.GetBullet() != null) attacker.StartRangedAttackSequence();
            else
            {
                _departure = attacker.transform.position;
                _destination = target.transform.position + target.transform.forward;    // �Ώۂ̑O��1m��ڕW�n�_�ɂ���
                attacker.StartClosedAttackSequence();
            }

            // �^�[�Q�b�g���K�[�h�X�L���g�p���̓K�[�h���[�V�������Đ�
            if (target.IsSkillInUse(SkillsData.ID.SKILL_GUARD)) target.SetAnimator(AnimDatas.ANIME_CONDITIONS_TAG.GUARD, true);
        }

        /// <summary>
        /// ��U���L��������̃J�E���^�[�������J�n���܂�
        /// </summary>
        /// <param name="attacker">�U���L�����N�^�[</param>
        /// <param name="target">��U���L�����N�^�[</param>
        private void StartCounter(Character attacker, Character target)
        {
            // �_���[�W�\�����Z�b�g
            _btlMgr.ApplyDamageExpect(target, attacker);

            // �U���L�����Ɣ�U���L���������ւ��ĊJ�n
            StartAttack(target, attacker);
        }

        /// <summary>
        /// �U���L�����Ɣ�U���L�����Ԃ̍X�V�������p���B�p�̂��̂ɐ؂�ւ��܂�
        /// </summary>
        /// <param name="attacker">�U���L�����N�^�[</param>
        /// <param name="target">��U���L�����N�^�[</param>
        private void ToggleParryUpdate(Character attacker, Character target)
        {
            // �X�V�p�֐���؂�ւ�
            _updateAttackerAttack   = _attackCharacter.UpdateParryOnAttacker;
            _updateTargetAttack     = _targetCharacter.UpdateParryOnTargeter;
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
            Transform allyTransform     = ally.transform;
            Transform opponentTransform = opponent.transform;
            allyTransform.position      = centralPos + new Vector3(0f, 0f, -_stageCtrl.BattlePosLengthFromCentral);
            opponentTransform.position  = centralPos + new Vector3(0f, 0f, _stageCtrl.BattlePosLengthFromCentral);
            allyTransform.rotation      = Quaternion.LookRotation(centralPos - allyTransform.position);
            opponentTransform.rotation  = Quaternion.LookRotation(centralPos - opponentTransform.position);
            // �J�����p�����[�^��퓬�t�B�[���h�p�ɐݒ�
            _btlCamCtrl.AdaptBattleFieldSetting();
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