using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Unity.IO.LowLevel.Unsafe.AsyncReadManagerMetrics;

public class EMAIAggressive : EMAIBase
{
    override protected float TARGET_ATTACK_BASE_VALUE { get; } = 50;
    override protected float WITHIN_RANGE_VALUE { get; } = 50;

    /// <summary>
    /// 各グリッドの評価値を計算します
    /// </summary>
    /// <param name="param">自身のパラメータ</param>
    /// <param name="tmpParam">自身の一時保持パラメータ</param>
    override public (bool, bool) DetermineDestinationAndTarget( in Character.Parameter selfParam, in Character.TmpParameter selfTmpParam )
    {
        ResetDestinationAndTarget();

        var stageGrid                   = StageGrid.Instance;
        List<int> candidateRouteIndexs  = new List<int>(stageGrid.GridTotalNum);

        List<(int gridIndex, List<CharacterHashtable.Key> opponents)> candidates;

        // 攻撃範囲内に敵対キャラクターが存在するか確認し、存在する場合はそのキャラクター達を取得
        if (CheckExistTargetInRange(selfParam, selfTmpParam, out candidates))
        {
            (int gridIndex,　Character target, float eValue) maxEvaluate = (-1, null, int.MinValue);

            // 戦闘結果評価が最も高い相手を求める
            foreach (var candidate in candidates)
            {
                foreach (var opponent in candidate.opponents)
                {
                    var character = BattleManager.Instance.GetCharacterFromHashtable(opponent);
                    if (character == null) continue;
                    var eValue = CalcurateEvaluateAttack(selfParam, character.param);
                    if (maxEvaluate.eValue < eValue)
                    {
                        maxEvaluate = (candidate.gridIndex, character, eValue);
                    }
                }
            }

            // 評価値の高い位置と相手を目標移動位置、攻撃対象に設定
            _destinationGridIndex   = maxEvaluate.gridIndex;
            _targetCharacter        = maxEvaluate.target;

            // 進行可能グリッドをルート候補に挿入
            for (int i = 0; i < stageGrid.GridTotalNum; ++i)
            {
                if ( 0 <= stageGrid.GetGridInfo(i).estimatedMoveRange)
                {
                    candidateRouteIndexs.Add(i);
                }
            }
            candidateRouteIndexs.Add(selfTmpParam.gridIndex);   // 現在地点も挿入
            _proposedMoveRoute = stageGrid.ExtractShortestRouteIndexs(selfTmpParam.gridIndex, _destinationGridIndex, candidateRouteIndexs);
        }
        // 攻撃範囲内に敵対キャラクターが存在しない場合は、評価値を計算して最も高いグリッド位置へ向かうように
        else
        {
            // 最大評価ルート保存用
            (List<(int routeIndex, int routeCost)> route, float evaluateValue) maxEvaluateRoute = (null, float.MinValue);

            // 進行可能な全てのグリッドを探索候補に加える
            var flag = StageGrid.BitFlag.CANNOT_MOVE | StageGrid.BitFlag.PLAYER_EXIST | StageGrid.BitFlag.OTHER_EXIST;
            for (int i = 0; i < stageGrid.GridTotalNum; ++i)
            {
                if (!Methods.CheckBitFlag(stageGrid.GetGridInfo(i).flag, flag))
                {
                    candidateRouteIndexs.Add(i);
                }
            }

            // 各プレイヤーが存在するグリッドの評価値を計算する
            foreach (Player player in BattleManager.Instance.GetPlayerEnumerable())
            {
                int destGridIndex       = player.tmpParam.gridIndex;
                ref float evaluateValue = ref _gridEvaluationValues[destGridIndex];

                // 目的座標にはキャラクターがいるため、候補ルートから既に除かれているので加える
                candidateRouteIndexs.Add(destGridIndex);

                // 攻撃による評価値を加算
                evaluateValue += CalcurateEvaluateAttack(selfParam, player.param);

                // 経路コストの逆数を乗算(経路コストが低いほど評価値を大きくするため)
                List<(int routeIndexs, int routeCost)> route = stageGrid.ExtractShortestRouteIndexs(selfTmpParam.gridIndex, destGridIndex, candidateRouteIndexs);
                int totalCost = route[^1].routeCost;    // ^1は最後の要素のインデックス(C#8.0以降から使用可能)
                evaluateValue *= 1f / totalCost;

                // 最も評価の高いルートを保存
                if (maxEvaluateRoute.evaluateValue < evaluateValue)
                {
                    maxEvaluateRoute = (route, evaluateValue);
                }
            }

            // 最も高い評価値のルートのうち、最大限の移動レンジで進んだグリッドへ向かうように設定
            int range           = selfParam.moveRange;
            int prevCost        = 0;   // routeCostは各インデックスまでの合計値コストなので、差分を得る必要がある
            _proposedMoveRoute  = maxEvaluateRoute.route;

            foreach ((int routeIndex, int routeCost) r in _proposedMoveRoute)
            {
                range -= (r.routeCost - prevCost);
                prevCost = r.routeCost;

                if (range < 0) break;

                // グリッド上にキャラクターが存在しないことを確認
                if (!StageGrid.Instance.GetGridInfo(r.routeIndex).IsExistCharacter()) _destinationGridIndex = r.routeIndex;
            }
        }

        int removeBaseIndex = _proposedMoveRoute.FindIndex(item => item.routeIndex == _destinationGridIndex) + 1;
        int removeCount     = _proposedMoveRoute.Count - removeBaseIndex;
        _proposedMoveRoute.RemoveRange(removeBaseIndex, removeCount);

        return ( IsValidDestination(), IsValidTarget() );
    }

    private bool CheckExistTargetInRange( Character.Parameter selfParam, Character.TmpParameter selfTmpParam, out List<(int gridIndex, List<CharacterHashtable.Key> opponents)> candidates)
    {
        var stageGrid = StageGrid.Instance;

        candidates = new List<(int gridIndex, List<CharacterHashtable.Key> opponents)>(Constants.CHARACTER_MAX_NUM);

        // 自身の移動範囲をステージ上に登録する
        bool isAttackable = !selfTmpParam.isEndCommand[(int)Character.BaseCommand.COMMAND_ATTACK];
        stageGrid.RegistMoveableInfo(selfTmpParam.gridIndex, selfParam.moveRange, selfParam.attackRange, selfParam.characterTag, isAttackable);

        for (int i = 0; i < stageGrid.GridTotalNum; ++i )
        {
            var info = stageGrid.GetGridInfo(i);
            // 攻撃可能地点かつキャラクターが存在していないグリッドを取得
            if ( Methods.CheckBitFlag( info.flag, StageGrid.BitFlag.TARGET_ATTACK_BASE ) && info.charaIndex < 0 )
            {
                // グリッドの十字方向に存在する敵対キャラクターを抽出
                List<CharacterHashtable.Key> opponentKeys;
                ExtractAttackabkeOpponentIndexs(i, out opponentKeys);
                if( 0 < opponentKeys.Count )
                {
                    candidates.Add((i, opponentKeys));
                }
            }
        }

        return 0 < candidates.Count;
    }


}