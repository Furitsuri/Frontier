using Frontier.Combat;
using Frontier.Entities;
using Frontier.Stage;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Frontier.Entities.Ai
{
    public class AiAggressive : AiBase
    {
        override protected float ATTACKABLE_VALUE { get; } = 50;
        override protected float WITHIN_RANGE_VALUE { get; } = 50;

        /// <summary>
        /// 攻撃対象が自身の攻撃範囲に存在するかを取得します
        /// </summary>
        /// <param name="selfParam">自身のパラメータ</param>
        /// <param name="selfTmpParam">自身の一時保存パラメータ</param>
        /// <param name="candidates">攻撃範囲内となる攻撃対象候補</param>
        /// <returns>存在の是非</returns>
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
                if (Methods.CheckBitFlag(info.flag, Stage.StageController.BitFlag.ATTACKABLE) && ( info.charaIndex < 0 || info.charaIndex == selfParam.characterIndex ) )
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

        /// <summary>
        /// 各グリッドの評価値を計算します
        /// </summary>
        /// <param name="selfParam">自身のパラメータ</param>
        /// <param name="selfTmpParam">自身の一時パラメータ</param>
        /// <returns>有効となる目的地及び攻撃対象がそれぞれ設定されたか否か</returns>
        override public (bool, bool) DetermineDestinationAndTarget( in CharacterParameter selfParam, in TemporaryParameter selfTmpParam )
        {
            _isDetermined = true;

            List<(int gridIndex, List<CharacterHashtable.Key> opponents)> candidates;

            // 攻撃範囲内に敵対キャラクターが存在するか確認し、存在する場合はそのキャラクター達を取得
            if ( CheckExistTargetInRange( selfParam, selfTmpParam, out candidates ) )
            {
                DetermineDestinationAndTargetInAttackRange( selfParam, selfTmpParam, candidates );
            }
            // 攻撃範囲内に敵対キャラクターが存在しない場合は、評価値を計算して最も高いグリッド位置へ向かうように
            else
            {
                DetermineDestinationAndTargetOutOfAttackRange( selfParam, selfTmpParam );
            }

            ResetAllTileEvaluationValues(); // 必ず評価値をリセットしてから終了する

            return (IsValidDestination(), IsValidTarget());
        }

        /// <summary>
        /// 進行予定の移動ルートを取得する際、自身の攻撃範囲に攻撃可能キャラクターが居た場合の処理を行います
        /// </summary>
        /// <param name="selfParam">自身のパラメータ</param>
        /// <param name="selfTmpParam">自身の一時パラメータ</param>
        override protected void DetermineDestinationAndTargetInAttackRange( in CharacterParameter selfParam, in TemporaryParameter selfTmpParam, List<(int gridIndex, List<CharacterHashtable.Key> opponents)> candidates )
        {
            (int gridIndex, Character target, float eValue) maxEvaluate = (-1, null, int.MinValue);

            // 戦闘結果の評価が最も高い相手を求める
            foreach ( var candidate in candidates )
            {
                foreach ( var opponent in candidate.opponents )
                {
                    var character = _btlRtnCtrl.BtlCharaCdr.GetCharacterFromHashtable(opponent);
                    if ( character == null ) continue;

                    var eValue = CalcurateEvaluateAttack(selfParam, character.Params.CharacterParam);
                    if ( maxEvaluate.eValue < eValue )
                    {
                        maxEvaluate = (candidate.gridIndex, character, eValue);
                    }
                }
            }

            // 評価値の高い位置と相手を目標移動位置、攻撃対象に設定
            _destinationGridIndex = maxEvaluate.gridIndex;
            _targetCharacter = maxEvaluate.target;

            // 現在移動可能なタイルと、自身が現在存在するタイルをルート候補とする条件
            Func<int, object[], bool> condition = (index, args) =>
            {
                var tileInfo    = _stageCtrl.GetGridInfo( index );
                var flag        = StageController.BitFlag.CANNOT_MOVE | StageController.BitFlag.ALLY_EXIST |  StageController.BitFlag.OTHER_EXIST;
                bool ownerExist = (index == ((TemporaryParameter)args[0]).gridIndex);

                return ( 0 <= tileInfo.estimatedMoveRange && ( ownerExist || !Methods.CheckBitFlag( tileInfo.flag, flag ) ) );
            };

            MovePathHandler.SetUpCandidateRouteIndexs( true, condition, selfTmpParam );
            MovePathHandler.FindMoveRoute( selfTmpParam.gridIndex, _destinationGridIndex );
        }

        /// <summary>
        /// 進行予定の移動ルートを取得する際、自身の攻撃範囲に攻撃可能キャラクターが居ない場合の処理を行います
        /// </summary>
        /// <param name="selfParam">自身のパラメータ</param>
        /// <param name="selfTmpParam">自身の一時パラメータ</param>
        override protected void DetermineDestinationAndTargetOutOfAttackRange( in CharacterParameter selfParam, in TemporaryParameter selfTmpParam )
        {
            // 最大評価ルート保存用
            (List<(int routeIndex, int routeCost, Vector3 TilePosition)> route, float evaluateValue) maxEvaluateRoute = (null, float.MinValue);

            // 移動値を無視した上で、進行可能なタイルをルート候補とする条件
            Func<int, object[], bool> condition = (index, args) =>
            {
                var flag        = Stage.StageController.BitFlag.CANNOT_MOVE;
                var tileInfo    = _stageCtrl.GetGridInfo( index );
                return ( !Methods.CheckBitFlag( tileInfo.flag, flag ) );
            };
            MovePathHandler.SetUpCandidateRouteIndexs( true, condition );

            // 各プレイヤーが存在するグリッドの評価値を計算する
            foreach ( Character chara in _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable( CHARACTER_TAG.PLAYER, CHARACTER_TAG.OTHER ) )
            {
                int destGridIndex       = chara.Params.TmpParam.GetCurrentGridIndex();
                ref float evaluateValue = ref _gridEvaluationValues[destGridIndex];

                evaluateValue += CalcurateEvaluateAttack( selfParam, chara.Params.CharacterParam );  // 攻撃による評価値を加算

                // 経路コストの逆数を乗算(経路コストが低い、つまり近いターゲットほど評価値を大きくするため)
                if( !MovePathHandler.FindMoveRoute( selfTmpParam.gridIndex, destGridIndex ) )
                {
                    Debug.LogError("ルートの探索に失敗しました。出発インデックスや目的インデックスなどの設定を見直してください。");
                    continue;
                }
                var route       = MovePathHandler.ProposedMoveRoute.ToList();
                int totalCost   = route[^1].routeCost;    // ^1は最後の要素のインデックス(C#8.0以降から使用可能)
                evaluateValue *= 1f / totalCost;

                // 最も評価の高いルートを保存
                if ( maxEvaluateRoute.evaluateValue < evaluateValue ) { maxEvaluateRoute = (route, evaluateValue); }
            }

            MovePathHandler.FindNearestReachableTileRoute( selfTmpParam.gridIndex, maxEvaluateRoute.route[^1].routeIndex, selfParam.moveRange, out _destinationGridIndex );

            /*
            // 最も評価値の高いルートのうち、最大限の移動レンジで進んだグリッドへ向かうように設定
            int range           = selfParam.moveRange;
            int prevCost        = 0;   // routeCostは各インデックスまでの合計値コストなので、差分を得る必要がある
            _proposedMoveRoute  = maxEvaluateRoute.route;

            foreach ( (int routeIndex, int routeCost, Vector3 t) r in _proposedMoveRoute )
            {
                range -= ( r.routeCost - prevCost );
                prevCost = r.routeCost;

                if ( range < 0 ) break;

                // グリッド上にキャラクターが存在しないことを確認
                if ( !_stageCtrl.GetGridInfo( r.routeIndex ).IsExistCharacter() ) _destinationGridIndex = r.routeIndex;
            }

            // 目的地となるタイルのインデックス値より、後方のインデックス値のタイル情報をリストから削除
            int removeBaseIndex = _proposedMoveRoute.FindIndex(item => item.routeIndex == _destinationGridIndex) + 1;
            int removeCount     = _proposedMoveRoute.Count - removeBaseIndex;
            _proposedMoveRoute.RemoveRange( removeBaseIndex, removeCount );
            */
        }
    }
}