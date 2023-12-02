using Frontier.Stage;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

namespace Frontier
{
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

        protected BattleManager _btlMgr;
        protected StageController _stageCtrl;
        // ���Ɉړ��Ώۂ�U���Ώۂ����肵�Ă��邩
        protected bool _isDetermined = false;
        // �ړ��ڕW�O���b�h�̃C���f�b�N�X�l
        protected int _destinationGridIndex = -1;
        // �U���Ώۂ̃L�����N�^�[�̃C���f�b�N�X�l
        protected Character _targetCharacter = null;
        // �e�O���b�h�̕]���l
        protected float[] _gridEvaluationValues = null;
        // �U��(�ړ�)�\�͈͓��ɑ��݂���U���ΏۃL�����N�^�[
        protected List<TargetCandidateInfo> _targetChandidateInfos = null;
        // �i�s�o�H
        protected List<(int routeIndex, int routeCost)> _suggestedMoveRoute;

        virtual protected float ATTACKABLE_TARGET_VALUE { get; } = 0;
        virtual protected float WITHIN_RANGE_VALUE { get; } = 0;
        virtual protected float ENABLE_DEFEAT_VALUE { get; } = 0;

        /// <summary>
        /// ���������܂�
        /// </summary>
        virtual public void Init(Enemy mySelf, BattleManager btlMgr, StageController stgCtrl)
        {
            _btlMgr = btlMgr;
            _stageCtrl = stgCtrl;
            _gridEvaluationValues = new float[_stageCtrl.GridTotalNum];
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

        /// <summary>
        /// �i�s�\��̈ړ����[�g���擾���܂�
        /// </summary>
        /// <returns>�i�s�\��̈ړ����[�g���</returns>
        public List<(int routeIndex, int routeCost)> GetProposedMoveRoute()
        {
            return _suggestedMoveRoute;
        }

        /// <summary>
        /// �ړ��ڕW�ƍU���ΏۃL�����N�^�[�����Z�b�g���܂�
        /// TODO : �čs���X�L���Ȃǂ���������ꍇ�́A�Ώۂɍčs����K�������ۂɂ��̊֐����Ăяo���Ă�������
        /// </summary>
        public void ResetDestinationAndTarget()
        {
            _isDetermined = false;
            _destinationGridIndex = -1;
            _targetCharacter = null;
        }

        /// <summary>
        /// ���Ɉړ��Ώۂ�U���Ώۂ����肵�Ă��邩�ǂ����̏����擾���܂�
        /// </summary>
        /// <returns>����̗L��</returns>
        public bool IsDetermined() { return _isDetermined; }

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
            return (_targetCharacter != null && _targetCharacter.param.characterTag != Character.CHARACTER_TAG.ENEMY);
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

            // �^�_���[�W�����̂܂ܕ]���l�ɂ��Ďg�p
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
        protected void ExtractAttackabkeOpponentIndexs(int baseIndex, out List<CharacterHashtable.Key> opponentCharaIndexs)
        {
            opponentCharaIndexs = new List<CharacterHashtable.Key>(4);
;
            (int GridRowNum, int GridColumnNum) = _stageCtrl.GetGridNumsXZ();

            // �\�������̔���֐��ƃC���f�b�N�X���^�v���ɋl�ߍ���
            (Func<bool> lambda, int index)[] tuples = new (Func<bool>, int)[]
            {
            (() => baseIndex % GridRowNum != 0,                       baseIndex - 1),
            (() => (baseIndex + 1) % GridRowNum != 0,                 baseIndex + 1),
            (() => 0 <= (baseIndex - GridRowNum),                     baseIndex - GridRowNum),
            (() => (baseIndex + GridRowNum) < _stageCtrl.GridTotalNum, baseIndex + GridRowNum)
         };

            foreach (var tuple in tuples)
            {
                if (tuple.lambda())
                {
                    var gridInfo = _stageCtrl.GetGridInfo(tuple.index);
                    if (gridInfo.characterTag == Character.CHARACTER_TAG.PLAYER || gridInfo.characterTag == Character.CHARACTER_TAG.OTHER)
                    {
                        opponentCharaIndexs.Add(new CharacterHashtable.Key(gridInfo.characterTag, gridInfo.charaIndex));
                    }
                }
            }
        }

        virtual public (bool, bool) DetermineDestinationAndTarget(in Character.Parameter selfParam, in Character.TmpParameter selfTmpParam)
        {
            return (false, false);
        }

        /// <summary>
        /// �����ꂩ�̃^�[�Q�b�g�ɍU���\�ȃO���b�h�̕]���l��Ԃ��܂�
        /// </summary>
        /// <param name="info">�w��O���b�h���</param>
        /// <returns>�]���l</returns>
        virtual protected float GetEvaluateEnableTargetAttackBase(in Stage.GridInfo info) { return ATTACKABLE_TARGET_VALUE; }

        virtual protected float GetEvaluateEnableDefeat(in Stage.GridInfo info) { return ENABLE_DEFEAT_VALUE; }
    }
}