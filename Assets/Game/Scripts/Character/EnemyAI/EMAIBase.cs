using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class EMAIBase
{
    /// <summary>
    /// ���g�̍U��(�ړ�)�\�͈͓��ɑ��݂���U���ΏۃL�����N�^�[�̏��ł�
    /// </summary>
    public struct TargetCandidateInfo
    {
        public int gridIndex;
        public List<int> targetCharaIndexs;
    }

    // �ړ��ڕW�O���b�h�̃C���f�b�N�X�l
    protected int _destinationGridIndex = -1;
    // �U���Ώۂ̃L�����N�^�[�̃C���f�b�N�X�l
    protected Character _targetCharacter = null;
    // �e�O���b�h�̕]���l
    protected float[] _gridEvaluationValues = null;
    // �U��(�ړ�)�\�͈͓��ɑ��݂���U���ΏۃL�����N�^�[
    protected List<TargetCandidateInfo> _targetChandidateInfos = null;
    // �i�s�o�H
    protected List<(int routeIndex, int routeCost)> _proposedMoveRoute;

    virtual protected float TARGET_ATTACK_BASE_VALUE { get; } = 0;
    virtual protected float WITHIN_RANGE_VALUE { get; } = 0;
    virtual protected float ENABLE_DEFEAT_VALUE { get; } = 0;

    /// <summary>
    /// ���������܂�
    /// </summary>
    virtual public void Init( Enemy mySelf )
    {
        _gridEvaluationValues = new float[StageGrid.Instance.GridTotalNum];
        _targetChandidateInfos = new List<TargetCandidateInfo>(64);
    }

    /// <summary>
    /// �ړI�n�̃O���b�h�C���f�b�N�X���擾���܂�
    /// </summary>
    /// <returns>�ړI�n�̃O���b�h�C���f�b�N�X</returns>
    public int GetDestinationGridIndex()
    {
        return _destinationGridIndex;
    }

    /// <summary>
    /// �U���Ώۂ̃L�����N�^�[���擾���܂�
    /// </summary>
    /// <returns>�U���Ώۂ̃L�����N�^�[</returns>
    public Character GetTargetCharacter()
    {
        return _targetCharacter;
    }

    public List<(int routeIndex, int routeCost)> GetProposedMoveRoute()
    {
        return _proposedMoveRoute;
    }

    /// <summary>
    /// �ړ��ڕW�ƍU���ΏۃL�����N�^�[�����Z�b�g���܂�
    /// </summary>
    public void ResetDestinationAndTarget()
    {
        _destinationGridIndex   = -1;
        _targetCharacter        = null;
    }

    /// <summary>
    /// �ړ��ڕW���L�����𔻒肵�܂�
    /// </summary>
    /// <returns>�L�����ۂ�</returns>
    public bool IsValidDestination()
    {
        return 0 <= _destinationGridIndex;
    }

    /// <summary>
    /// �U���Ώۂ��L�����𔻒肵�܂�
    /// </summary>
    /// <returns>�L�����ۂ�</returns>
    public bool IsValidTarget()
    {
        return _targetCharacter != null;
    }

    /// <summary>
    /// �U���ΏۃL�����N�^�[�C���f�b�N�X�l���L�����𔻒肵�܂�
    /// </summary>
    /// <returns>�L�����ۂ�</returns>
    public bool IsValidTargetCharacterIndex()
    {
        return (_targetCharacter != null && _targetCharacter.param.characterTag != Character.CHARACTER_TAG.CHARACTER_ENEMY);
    }

    /// <summary>
    /// �Ώۂ̃L�����N�^�[���U�������ۂ̕]���l���v�Z���܂�
    /// </summary>
    /// <param name="mySelf">���g</param>
    /// <param name="TargetCharacter">�Ώۂ̃L�����N�^�[</param>
    /// <returns>�]���l</returns>
    protected float CalcurateEvaluateAttack(in Character.Parameter selfParam, in Character.Parameter targetParam)
    {
        float evaluateValue = 0f;

        // �^�_���[�W�����̂܂ܕ]���l�����Ďg�p
        evaluateValue = Mathf.Max(0, selfParam.Atk - targetParam.Def);

        // �|�����Ƃ��o����ꍇ�̓{�[�i�X�����Z
        if (targetParam.CurHP <= evaluateValue) evaluateValue += ENABLE_DEFEAT_VALUE;

        return evaluateValue;
    }

    /// <summary>
    /// �w��C���f�b�N�X�̏\�������ɂ���G�΃L�����N�^�[�̃L�����N�^�[�C���f�b�N�X�𒊏o���܂�
    /// </summary>
    /// <param name="baseIndex">�w��C���f�b�N�X(�\�������̒��S�C���f�b�N�X)</param>
    /// <param name="opponentCharaIndexs">�����o���Ɏg�p���郊�X�g</param>
    protected void ExtractAttackabkeOpponentIndexs( int baseIndex, out List<CharacterHashtable.Key> opponentCharaIndexs )
    {
        opponentCharaIndexs = new List<CharacterHashtable.Key>(4);

        var stageGrid = StageGrid.Instance;
        (int gridNumX, int gridNumZ) = stageGrid.GetGridNumsXZ();

        // �\�������̔���֐��ƃC���f�b�N�X���^�v���ɋl�ߍ���
        (Func<bool> lambda, int index)[] tuples = new (Func<bool>, int )[]
        {
            (() => baseIndex % gridNumX != 0,                       baseIndex - 1),
            (() => (baseIndex + 1) % gridNumX != 0,                 baseIndex + 1),
            (() => 0 <= (baseIndex - gridNumX),                     baseIndex - gridNumX),
            (() => (baseIndex + gridNumX) < stageGrid.GridTotalNum, baseIndex + gridNumX)
        };

        foreach (var tuple in tuples)
        {
            if (tuple.lambda())
            {
                var gridInfo = stageGrid.GetGridInfo(tuple.index);
                if (gridInfo.characterTag == Character.CHARACTER_TAG.CHARACTER_PLAYER || gridInfo.characterTag == Character.CHARACTER_TAG.CHARACTER_OTHER)
                {
                    opponentCharaIndexs.Add(new CharacterHashtable.Key(gridInfo.characterTag, gridInfo.charaIndex));
                }
            }
        }
    }

    virtual public ( bool, bool ) DetermineDestinationAndTarget(in Character.Parameter selfParam, in Character.TmpParameter selfTmpParam)
    {
        return (false, false);
    }

    /// <summary>
    /// �����ꂩ�̃^�[�Q�b�g�ɍU���\�ȃO���b�h�̕]���l��Ԃ��܂�
    /// </summary>
    /// <param name="info">�w��O���b�h���</param>
    /// <returns>�]���l</returns>
    virtual protected float GetEvaluateEnableTargetAttackBase(in StageGrid.GridInfo info) { return TARGET_ATTACK_BASE_VALUE; }

    virtual protected float GetEvaluateEnableDefeat(in StageGrid.GridInfo info) { return ENABLE_DEFEAT_VALUE; }
}