using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TMPro.Examples;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

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
    private Character _attackCharacter              = null;
    private Character _targetCharacter              = null;
    private Character _diedCharacter                = null;
    // Transform�͒x�����߃L���b�V��
    private Transform _atkCharaTransform            = null;
    private Transform _tgtCharaTransform            = null;
    private Vector3 _departure                      = Vector3.zero;
    private Vector3 _destination                    = Vector3.zero;
    private Quaternion _atkCharaInitialRot          = Quaternion.identity;
    private Quaternion _tgtCharaInitialRot          = Quaternion.identity;
    private float _elapsedTime                      = 0f;
    private Character.ANIME_TAG[] _attackAnimTags   = new Character.ANIME_TAG[3] { Character.ANIME_TAG.SINGLE_ATTACK, Character.ANIME_TAG.DOUBLE_ATTACK, Character.ANIME_TAG.TRIPLE_ATTACK };
    private bool _CounterConditions                 = false;
    private UpdateAttack _updateAttackerAttack      = null;
    private UpdateAttack _updateTargetAttack        = null;

    public void Init(Character attackChara, Character targetChara)
    {
        _attackCharacter        = attackChara;
        _targetCharacter        = targetChara;
        _diedCharacter          = null;
        _atkCharaTransform      = _attackCharacter.transform;
        _tgtCharaTransform      = _targetCharacter.transform;
        _atkCharaInitialRot     = _atkCharaTransform.rotation;
        _tgtCharaInitialRot     = _tgtCharaTransform.rotation;
        _elapsedTime            = 0f;
        _phase                  = Phase.START;
        // �ΐ푊��Ƃ��Đݒ�
        _attackCharacter.SetOpponentCharacter(_targetCharacter);
        _targetCharacter.SetOpponentCharacter(_attackCharacter);
        // �J�E���^�[�����̐ݒ�
        _CounterConditions      = (_targetCharacter.IsSkillInUse(SkillsData.ID.SKILL_COUNTER));
        // �U���X�V�����̏����ʐݒ�
        if (_CounterConditions && _attackCharacter.GetBullet() != null ) _CounterConditions = _targetCharacter.GetBullet() != null;
        if (_attackCharacter.GetBullet() == null) _updateAttackerAttack = _attackCharacter.UpdateClosedAttack;
        else _updateAttackerAttack = _attackCharacter.UpdateRangedAttack;
        if (_targetCharacter.GetBullet() == null) _updateTargetAttack = _targetCharacter.UpdateClosedAttack;
        else _updateTargetAttack = _targetCharacter.UpdateRangedAttack;

        BattleCameraController.Instance.StartAttackSequenceMode( attackChara, targetChara );
    }

    // Update is called once per frame
    public bool Update()
    {
        switch(_phase)
        {
            case Phase.START:
                // START_ROTATION_TIME���o�߂���܂Ō�����ύX���܂�
                _elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(_elapsedTime/ Constants.ATTACK_ROTATIION_TIME);
                t = Mathf.SmoothStep(0f, 1f, t);

                Quaternion destAttackerRot  = Quaternion.LookRotation(_tgtCharaTransform.position - _atkCharaTransform.position);
                Quaternion destTargetRot    = Quaternion.LookRotation(_atkCharaTransform.position - _tgtCharaTransform.position);
                _atkCharaTransform.rotation = Quaternion.Lerp(_atkCharaInitialRot, destAttackerRot, t);
                _tgtCharaTransform.rotation = Quaternion.Lerp(_tgtCharaInitialRot, destTargetRot, t);

                if( BattleCameraController.Instance.IsFadeAttack() )
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

                    // �K�[�h�X�L���g�p���̓K�[�h���[�V�������Đ�
                    if ( _targetCharacter.IsSkillInUse( SkillsData.ID.SKILL_GUARD ) ) _targetCharacter.setAnimator(Character.ANIME_TAG.GUARD, true);
                    // �p���B�X�L���g�p���̓p���B�����p�����֑J��
                    if (_targetCharacter.IsSkillInUse(SkillsData.ID.SKILL_PARRY)) _phase = Phase.PARRY;
                    else
                    {
                        _phase = Phase.ATTACK;
                    }
                }
                break;
            case Phase.ATTACK:
                if(_updateAttackerAttack(_departure, _destination))
                {
                    // �J�����ΏۂƃJ�����p�����[�^��ύX
                    BattleCameraController.Instance.TransitNextPhaseCameraParam(null, _targetCharacter.transform);
                    // �K�[�h�X�L�����g�p���̓K�[�h���[�V������߂�
                    if (_targetCharacter.IsSkillInUse(SkillsData.ID.SKILL_GUARD)) _targetCharacter.setAnimator(Character.ANIME_TAG.GUARD, false);
                    // �Ώۂ����S���Ă���ꍇ�͎��S������
                    if (_targetCharacter.IsDead())
                    {
                        _diedCharacter = _targetCharacter;
                        _phase = Phase.DIE;
                    }
                    else if (_CounterConditions)
                    {
                        BattleManager.Instance.ApplyDamageExpect(_targetCharacter, _attackCharacter);
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
                    BattleCameraController.Instance.TransitNextPhaseCameraParam(null, _targetCharacter.transform);

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
                    var info = StageGrid.Instance.GetGridInfo( _attackCharacter.tmpParam.gridIndex );
                    BattleCameraController.Instance.EndAttackSequenceMode(_attackCharacter);

                    _phase = Phase.END;
                }
                break;
            case Phase.END:
                if (BattleCameraController.Instance.IsFadeEnd())
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
        var tag = _attackAnimTags[attacker.skillModifiedParam.AtkNum - 1];
        if (attacker.GetBullet() != null) attacker.setAnimator(tag);
        else
        {
            _departure      = attacker.transform.position;
            _destination    = target.transform.position + target.transform.forward;
            attacker.PlayClosedAttack();
        }
    }

    /// <summary>
    /// �퓬�t�B�[���h�ɑJ�ڂ��܂�
    /// </summary>
    /// <param name="attacker">�U���L�����N�^�[</param>
    /// <param name="target">��U���L�����N�^�[</param>
    private void TransitBattleField(Character attacker, Character target)
    {
        var stgGrid = StageGrid.Instance;

        // ���b�V���y��attaker��target�ȊO�̃L�����N�^�[���\����
        stgGrid.ToggleMeshDisplay(false);
        foreach (var player in BattleManager.Instance.GetPlayerEnumerable())
        {
            if (player != attacker && player != target)
            {
                player.gameObject.SetActive(false);
            }
        }
        foreach (var enemy in BattleManager.Instance.GetEnemyEnumerable())
        {
            if (enemy != attacker && enemy != target)
            {
                enemy.gameObject.SetActive(false);
            }
        }

        // �L�����N�^�[���X�e�[�W�̒��S�ʒu���炻�ꂼ�ꗣ�ꂽ�ꏊ�ɗ�������
        var centralPos = stgGrid.transform.position;

        // �����ƓG�Α��ŕ���
        Character ally  = null;
        Character opponent = null;
        if( attacker.IsPlayer())
        {
            ally        = attacker;
            opponent    = target;
        }
        else
        {
            if( target.IsPlayer() )
            {
                ally        = target;
                opponent    = attacker;
            }
            else if( target.IsOther() )
            {
                ally        = target;
                opponent    = attacker;
            }
            else
            {
                ally        = attacker;
                opponent    = target;
            }
        }
        
        // �����͉��s��O���A�G�͉��s�����̗����ʒu�Ƃ���
        Transform allyTransform     = ally.transform;
        Transform opponentTransform = opponent.transform;
        allyTransform.position      = centralPos + new Vector3(0f, 0f, -stgGrid.BattlePosLengthFromCentral);
        opponentTransform.position  = centralPos + new Vector3(0f, 0f, stgGrid.BattlePosLengthFromCentral);
        allyTransform.rotation      = Quaternion.LookRotation(centralPos - allyTransform.position);
        opponentTransform.rotation  = Quaternion.LookRotation(centralPos - opponentTransform.position);
    }

    /// <summary>
    /// �X�e�[�W�t�B�[���h�ɑJ�ڂ��܂�
    /// </summary>
    /// <param name="attacker">�U���L�����N�^�[</param>
    /// <param name="target">��U���L�����N�^�[</param>
    private void TransitStageField(Character attacker, Character target)
    {
        var stgGrid = StageGrid.Instance;

        // ��\���ɂ��Ă������̂�\��
        stgGrid.ToggleMeshDisplay(true);
        foreach (var player in BattleManager.Instance.GetPlayerEnumerable())
        {
            player.gameObject.SetActive(true);
        }
        foreach (var enemy in BattleManager.Instance.GetEnemyEnumerable())
        {
            enemy.gameObject.SetActive(true);
        }

        // �L�����N�^�[���X�e�[�W�̒��S�ʒu���炻�ꂼ�ꗣ�ꂽ�ꏊ�ɗ�������
        var info = StageGrid.Instance.GetGridInfo(attacker.tmpParam.gridIndex);
        _attackCharacter.transform.position = info.charaStandPos;
        _attackCharacter.transform.rotation = _atkCharaInitialRot;
        info = StageGrid.Instance.GetGridInfo(target.tmpParam.gridIndex);
        _targetCharacter.transform.position = info.charaStandPos;
        _targetCharacter.transform.rotation = _tgtCharaInitialRot;
    }
}
