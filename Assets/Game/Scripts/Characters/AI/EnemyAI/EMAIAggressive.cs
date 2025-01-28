using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Frontier
{
    public class EMAIAggressive : EMAIBase
    {
        override protected float ATTACKABLE_TARGET_VALUE { get; } = 50;
        override protected float WITHIN_RANGE_VALUE { get; } = 50;

        /// <summary>
        /// �e�O���b�h�̕]���l���v�Z���܂�
        /// </summary>
        /// <param name="selfParam">���g�̃p�����[�^</param>
        /// <param name="selfTmpParam">���g�̈ꎞ�p�����[�^</param>
        /// <returns>�L���ƂȂ�ړI�n�y�эU���Ώۂ����ꂼ��ݒ肳�ꂽ���ۂ�</returns>
        override public (bool, bool) DetermineDestinationAndTarget(in Character.Parameter selfParam, in Character.TmpParameter selfTmpParam)
        {
            _isDetermined = true;

            List<int> candidateRouteIndexs = new List<int>(_stageCtrl.GridTotalNum);

            List<(int gridIndex, List<CharacterHashtable.Key> opponents)> candidates;

            // �U���͈͓��ɓG�΃L�����N�^�[�����݂��邩�m�F���A���݂���ꍇ�͂��̃L�����N�^�[�B���擾
            if (CheckExistTargetInRange(selfParam, selfTmpParam, out candidates))
            {
                (int gridIndex, Character target, float eValue) maxEvaluate = (-1, null, int.MinValue);

                // �퓬���ʕ]�����ł�������������߂�
                foreach (var candidate in candidates)
                {
                    foreach (var opponent in candidate.opponents)
                    {
                        var character = _btlMgr.BtlCharaCdr.GetCharacterFromHashtable(opponent);

                        if (character == null) continue;
                        var eValue = CalcurateEvaluateAttack(selfParam, character.param);
                        if (maxEvaluate.eValue < eValue)
                        {
                            maxEvaluate = (candidate.gridIndex, character, eValue);
                        }
                    }
                }

                // �]���l�̍����ʒu�Ƒ����ڕW�ړ��ʒu�A�U���Ώۂɐݒ�
                _destinationGridIndex = maxEvaluate.gridIndex;
                _targetCharacter = maxEvaluate.target;

                // �i�s�\�O���b�h�����[�g���ɑ}��
                for (int i = 0; i < _stageCtrl.GridTotalNum; ++i)
                {
                    if (0 <= _stageCtrl.GetGridInfo(i).estimatedMoveRange)
                    {
                        candidateRouteIndexs.Add(i);
                    }
                }
                candidateRouteIndexs.Add(selfTmpParam.gridIndex);   // ���ݒn�_���}��
                _suggestedMoveRoute = _stageCtrl.ExtractShortestRouteIndexs(selfTmpParam.gridIndex, _destinationGridIndex, candidateRouteIndexs);
            }
            // �U���͈͓��ɓG�΃L�����N�^�[�����݂��Ȃ��ꍇ�́A�]���l���v�Z���čł������O���b�h�ʒu�֌������悤��
            else
            {
                // �ő�]�����[�g�ۑ��p
                (List<(int routeIndex, int routeCost)> route, float evaluateValue) maxEvaluateRoute = (null, float.MinValue);

                // �i�s�\�ȑS�ẴO���b�h��T�����ɉ�����
                var flag = Stage.StageController.BitFlag.CANNOT_MOVE | Stage.StageController.BitFlag.PLAYER_EXIST | Stage.StageController.BitFlag.OTHER_EXIST;
                for (int i = 0; i < _stageCtrl.GridTotalNum; ++i)
                {
                    if (!Methods.CheckBitFlag(_stageCtrl.GetGridInfo(i).flag, flag))
                    {
                        candidateRouteIndexs.Add(i);
                    }
                }

                // �e�v���C���[�����݂���O���b�h�̕]���l���v�Z����
                foreach (Player player in _btlMgr.BtlCharaCdr.GetCharacterEnumerable(Character.CHARACTER_TAG.PLAYER))
                {
                    int destGridIndex = player.tmpParam.gridIndex;
                    ref float evaluateValue = ref _gridEvaluationValues[destGridIndex];

                    // �ړI���W�ɂ̓L�����N�^�[�����邽�߁A��⃋�[�g������ɏ�����Ă���̂ŉ�����
                    candidateRouteIndexs.Add(destGridIndex);

                    // �U���ɂ��]���l�����Z
                    evaluateValue += CalcurateEvaluateAttack(selfParam, player.param);

                    // �o�H�R�X�g�̋t������Z(�o�H�R�X�g���Ⴂ�قǕ]���l��傫�����邽��)
                    List<(int routeIndexs, int routeCost)> route = _stageCtrl.ExtractShortestRouteIndexs(selfTmpParam.gridIndex, destGridIndex, candidateRouteIndexs);
                    int totalCost = route[^1].routeCost;    // ^1�͍Ō�̗v�f�̃C���f�b�N�X(C#8.0�ȍ~����g�p�\)
                    evaluateValue *= 1f / totalCost;

                    // �ł��]���̍������[�g��ۑ�
                    if (maxEvaluateRoute.evaluateValue < evaluateValue)
                    {
                        maxEvaluateRoute = (route, evaluateValue);
                    }
                }

                // �ł������]���l�̃��[�g�̂����A�ő���̈ړ������W�Ői�񂾃O���b�h�֌������悤�ɐݒ�
                int range = selfParam.moveRange;
                int prevCost = 0;   // routeCost�͊e�C���f�b�N�X�܂ł̍��v�l�R�X�g�Ȃ̂ŁA�����𓾂�K�v������
                _suggestedMoveRoute = maxEvaluateRoute.route;

                foreach ((int routeIndex, int routeCost) r in _suggestedMoveRoute)
                {
                    range -= (r.routeCost - prevCost);
                    prevCost = r.routeCost;

                    if (range < 0) break;

                    // �O���b�h��ɃL�����N�^�[�����݂��Ȃ����Ƃ��m�F
                    if (!_stageCtrl.GetGridInfo(r.routeIndex).IsExistCharacter()) _destinationGridIndex = r.routeIndex;
                }
            }

            int removeBaseIndex = _suggestedMoveRoute.FindIndex(item => item.routeIndex == _destinationGridIndex) + 1;
            int removeCount = _suggestedMoveRoute.Count - removeBaseIndex;
            _suggestedMoveRoute.RemoveRange(removeBaseIndex, removeCount);

            return (IsValidDestination(), IsValidTarget());
        }

        private bool CheckExistTargetInRange(Character.Parameter selfParam, Character.TmpParameter selfTmpParam, out List<(int gridIndex, List<CharacterHashtable.Key> opponents)> candidates)
        {
            candidates = new List<(int gridIndex, List<CharacterHashtable.Key> opponents)>(Constants.CHARACTER_MAX_NUM);

            // ���g�̈ړ��͈͂��X�e�[�W��ɓo�^����
            bool isAttackable = !selfTmpParam.isEndCommand[(int)Character.Command.COMMAND_TAG.ATTACK];
            _stageCtrl.RegistMoveableInfo(selfTmpParam.gridIndex, selfParam.moveRange, selfParam.attackRange, selfParam.characterIndex, selfParam.characterTag, isAttackable);

            for (int i = 0; i < _stageCtrl.GridTotalNum; ++i)
            {
                var info = _stageCtrl.GetGridInfo(i);
                // �U���\�n�_���L�����N�^�[�����݂��Ă��Ȃ�(�������g�͗L��)�O���b�h���擾
                if (Methods.CheckBitFlag(info.flag, Stage.StageController.BitFlag.ATTACKABLE_TARGET) && ( info.charaIndex < 0 || info.charaIndex == selfParam.characterIndex ) )
                {
                    // �O���b�h�̏\�������ɑ��݂���G�΃L�����N�^�[�𒊏o
                    List<CharacterHashtable.Key> opponentKeys;
                    ExtractAttackabkeOpponentIndexs(i, out opponentKeys);
                    if (0 < opponentKeys.Count)
                    {
                        candidates.Add((i, opponentKeys));
                    }
                }
            }

            return 0 < candidates.Count;
        }
    }
}