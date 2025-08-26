using Frontier.Combat;
using Frontier.Entities;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Frontier
{
    public class EmAiAggressive : EmAiBase
    {
        override protected float ATTACKABLE_TARGET_VALUE { get; } = 50;
        override protected float WITHIN_RANGE_VALUE { get; } = 50;

        /// <summary>
        /// 各グリッドの評価値を計算します
        /// </summary>
        /// <param name="selfParam">自身のパラメータ</param>
        /// <param name="selfTmpParam">自身の一時パラメータ</param>
        /// <returns>有効となる目的地及び攻撃対象がそれぞれ設定されたか否か</returns>
        override public (bool, bool) DetermineDestinationAndTarget(in CharacterParameter selfParam, in TemporaryParameter selfTmpParam)
        {
            _isDetermined = true;

            List<int> candidateRouteIndexs = new List<int>(_stageData.GetGridToralNum());

            List<(int gridIndex, List<CharacterHashtable.Key> opponents)> candidates;

            // 攻撃範囲内に敵対キャラクターが存在するか確認し、存在する場合はそのキャラクター達を取得
            if (CheckExistTargetInRange(selfParam, selfTmpParam, out candidates))
            {
                (int gridIndex, Character target, float eValue) maxEvaluate = (-1, null, int.MinValue);

                // 戦闘結果評価が最も高い相手を求める
                foreach (var candidate in candidates)
                {
                    foreach (var opponent in candidate.opponents)
                    {
                        var character = _btlRtnCtrl.BtlCharaCdr.GetCharacterFromHashtable(opponent);

                        if (character == null) continue;
                        var eValue = CalcurateEvaluateAttack(selfParam, character.Params.CharacterParam);
                        if (maxEvaluate.eValue < eValue)
                        {
                            maxEvaluate = (candidate.gridIndex, character, eValue);
                        }
                    }
                }

                // 評価値の高い位置と相手を目標移動位置、攻撃対象に設定
                _destinationGridIndex = maxEvaluate.gridIndex;
                _targetCharacter = maxEvaluate.target;

                // 進行可能グリッドをルート候補に挿入
                for (int i = 0; i < _stageData.GetGridToralNum(); ++i)
                {
                    if (0 <= _stageCtrl.GetGridInfo(i).estimatedMoveRange)
                    {
                        candidateRouteIndexs.Add(i);
                    }
                }
                candidateRouteIndexs.Add(selfTmpParam.gridIndex);   // 現在地点も挿入
                _suggestedMoveRoute = _stageCtrl.ExtractShortestRouteIndexs(selfTmpParam.gridIndex, _destinationGridIndex, candidateRouteIndexs);
            }
            // 攻撃範囲内に敵対キャラクターが存在しない場合は、評価値を計算して最も高いグリッド位置へ向かうように
            else
            {
                // 最大評価ルート保存用
                (List<(int routeIndex, int routeCost)> route, float evaluateValue) maxEvaluateRoute = (null, float.MinValue);

                // 進行可能な全てのグリッドを探索候補に加える
                var flag = Stage.StageController.BitFlag.CANNOT_MOVE | Stage.StageController.BitFlag.PLAYER_EXIST | Stage.StageController.BitFlag.OTHER_EXIST;
                for (int i = 0; i < _stageData.GetGridToralNum(); ++i)
                {
                    if (!Methods.CheckBitFlag(_stageCtrl.GetGridInfo(i).flag, flag))
                    {
                        candidateRouteIndexs.Add(i);
                    }
                }

                // 各プレイヤーが存在するグリッドの評価値を計算する
                foreach (Player player in _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable(CHARACTER_TAG.PLAYER))
                {
                    int destGridIndex = player.Params.TmpParam.GetCurrentGridIndex();
                    ref float evaluateValue = ref _gridEvaluationValues[destGridIndex];

                    // 目的座標にはキャラクターがいるため、候補ルートから既に除かれているので加える
                    candidateRouteIndexs.Add(destGridIndex);

                    // 攻撃による評価値を加算
                    evaluateValue += CalcurateEvaluateAttack(selfParam, player.Params.CharacterParam);

                    // 経路コストの逆数を乗算(経路コストが低いほど評価値を大きくするため)
                    List<(int routeIndexs, int routeCost)> route = _stageCtrl.ExtractShortestRouteIndexs(selfTmpParam.gridIndex, destGridIndex, candidateRouteIndexs);
                    int totalCost = route[^1].routeCost;    // ^1は最後の要素のインデックス(C#8.0以降から使用可能)
                    evaluateValue *= 1f / totalCost;

                    // 最も評価の高いルートを保存
                    if (maxEvaluateRoute.evaluateValue < evaluateValue)
                    {
                        maxEvaluateRoute = (route, evaluateValue);
                    }
                }

                // 最も高い評価値のルートのうち、最大限の移動レンジで進んだグリッドへ向かうように設定
                int range = selfParam.moveRange;
                int prevCost = 0;   // routeCostは各インデックスまでの合計値コストなので、差分を得る必要がある
                _suggestedMoveRoute = maxEvaluateRoute.route;

                foreach ((int routeIndex, int routeCost) r in _suggestedMoveRoute)
                {
                    range -= (r.routeCost - prevCost);
                    prevCost = r.routeCost;

                    if (range < 0) break;

                    // グリッド上にキャラクターが存在しないことを確認
                    if (!_stageCtrl.GetGridInfo(r.routeIndex).IsExistCharacter()) _destinationGridIndex = r.routeIndex;
                }
            }

            int removeBaseIndex = _suggestedMoveRoute.FindIndex(item => item.routeIndex == _destinationGridIndex) + 1;
            int removeCount = _suggestedMoveRoute.Count - removeBaseIndex;
            _suggestedMoveRoute.RemoveRange(removeBaseIndex, removeCount);

            return (IsValidDestination(), IsValidTarget());
        }

        private bool CheckExistTargetInRange(CharacterParameter selfParam, TemporaryParameter selfTmpParam, out List<(int gridIndex, List<CharacterHashtable.Key> opponents)> candidates)
        {
            candidates = new List<(int gridIndex, List<CharacterHashtable.Key> opponents)>(Constants.CHARACTER_MAX_NUM);

            // 自身の移動範囲をステージ上に登録する
            bool isAttackable = !selfTmpParam.isEndCommand[(int)Command.COMMAND_TAG.ATTACK];
            _stageCtrl.RegistMoveableInfo(selfTmpParam.gridIndex, selfParam.moveRange, selfParam.attackRange, selfParam.characterIndex, selfParam.characterTag, isAttackable);

            for (int i = 0; i < _stageData.GetGridToralNum(); ++i)
            {
                var info = _stageCtrl.GetGridInfo(i);
                // 攻撃可能地点かつキャラクターが存在していない(自分自身は有効)グリッドを取得
                if (Methods.CheckBitFlag(info.flag, Stage.StageController.BitFlag.ATTACKABLE_TARGET) && ( info.charaIndex < 0 || info.charaIndex == selfParam.characterIndex ) )
                {
                    // グリッドの十字方向に存在する敵対キャラクターを抽出
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