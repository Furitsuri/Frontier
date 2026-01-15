using Frontier.Combat;
using Frontier.Entities;
using Frontier.Stage;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools;

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
        /// <returns>存在の有無</returns>
        private bool CollectTargetInActionableRange( CharacterParameter selfParam, TemporaryParameter selfTmpParam, in int[] ownerTileCosts, in CharacterKey ownerKey, out List<(int tileIndex, List<CharacterKey> opponents)> candidates )
        {
            candidates = new List<(int tileIndex, List<CharacterKey> opponents)>( Constants.CHARACTER_MAX_NUM );

            // 自身の移動範囲をステージ上に登録する
            bool isAttackable = !selfTmpParam.isEndCommand[( int ) COMMAND_TAG.ATTACK];
            float curHeight = _stageCtrl.GetTileStaticData( selfTmpParam.gridIndex ).Height;

            _owner.ActionRangeCtrl.SetupActionableRangeData( selfTmpParam.gridIndex, curHeight );
            _owner.ActionRangeCtrl.DrawActionableRange();

            // 攻撃可能範囲に攻撃対象がいるかどうかを判定
            for( int i = 0; i < _owner.ActionRangeCtrl.ActionableTileMap.AttackableTileMap.Count; ++i )
            {
                var tileIndex = _owner.ActionRangeCtrl.ActionableTileMap.AttackableTileMap.ElementAt( i ).Key;
                var tileDData = _owner.ActionRangeCtrl.ActionableTileMap.AttackableTileMap.ElementAt( i ).Value;

                // 移動可能地点、かつキャラクターが存在していない(自分自身は有効)タイルを取得
                if( 0 <= tileDData.EstimatedMoveRange && ( !tileDData.CharaKey.IsValid() || tileDData.CharaKey == ownerKey ) )
                {
                    // タイルの十字方向に存在する敵対キャラクターを抽出
                    List<CharacterKey> opponentKeys;
                    ExtractAttackabkeOpponentIndexs( tileIndex, ownerKey.CharacterTag, out opponentKeys );
                    if( 0 < opponentKeys.Count )
                    {
                        candidates.Add( (tileIndex, opponentKeys) );
                    }
                }
            }

            return ( 0 < candidates.Count );
        }

        /// <summary>
        /// 各グリッドの評価値を計算します
        /// </summary>
        /// <param name="selfParam">自身のパラメータ</param>
        /// <param name="selfTmpParam">自身の一時パラメータ</param>
        /// <returns>有効となる目的地及び攻撃対象がそれぞれ設定されたか否か</returns>
        public override (bool, bool) DetermineDestinationAndTarget( in CharacterParameters ownerParams, in int[] ownerTileCosts, in CharacterKey ownerKey )
        {
            _isDetermined = true;

            List<(int tileIndex, List<CharacterKey> opponents)> candidates = null;

            // 攻撃範囲内に敵対キャラクターが存在するか確認し、存在する場合はそのキャラクター達を取得
            if( CollectTargetInActionableRange( ownerParams.CharacterParam, ownerParams.TmpParam, in ownerTileCosts, in ownerKey, out candidates ) )
            {
                DetermineDestinationAndTargetInAttackRange( in ownerParams, in ownerTileCosts, candidates );
            }
            // 攻撃範囲内に敵対キャラクターが存在しない場合は、評価値を計算して最も高いグリッド位置へ向かうように
            else
            {
                DetermineDestinationAndTargetOutOfAttackRange( in ownerParams, in ownerTileCosts );
            }

            ResetAllTileEvaluationValues(); // 必ず評価値をリセットしてから終了する

            return (IsValidDestination(), IsValidTarget());
        }

        /// <summary>
        /// 進行予定の移動ルートを取得する際、自身の攻撃範囲に攻撃可能キャラクターが居た場合の処理を行います
        /// </summary>
        /// <param name="selfParam">自身のパラメータ</param>
        /// <param name="selfTmpParam">自身の一時パラメータ</param>
        override protected void DetermineDestinationAndTargetInAttackRange( in CharacterParameters ownerParams, in int[] ownerTileCosts, List<(int gridIndex, List<CharacterKey> opponents)> candidates )
        {
            (int gridIndex, Character target, float eValue) maxEvaluate = (-1, null, int.MinValue);

            // 戦闘結果の評価が最も高い相手を求める
            foreach( var candidate in candidates )
            {
                foreach( var opponent in candidate.opponents )
                {
                    var character = _btlRtnCtrl.BtlCharaCdr.GetCharacterFromDictionary( opponent );
                    if( character == null ) continue;

                    var eValue = CalcurateEvaluateAttack( ownerParams.CharacterParam, character.Params.CharacterParam );
                    if( maxEvaluate.eValue < eValue )
                    {
                        maxEvaluate = (candidate.gridIndex, character, eValue);
                    }
                }
            }

            // 評価値の高い位置と相手を目標移動位置、攻撃対象に設定
            _destinationTileIndex   = maxEvaluate.gridIndex;
            _targetCharacter        = maxEvaluate.target;

            _owner.ActionRangeCtrl.MovePathHdlr.FindMovePath( ownerParams.TmpParam.gridIndex, _destinationTileIndex, ownerParams.CharacterParam.jumpForce, ownerTileCosts, _owner.ActionRangeCtrl.ActionableTileMap.MoveableTileMap );
        }

        /// <summary>
        /// 進行予定の移動ルートを取得する際、自身の攻撃範囲に攻撃可能キャラクターが居ない場合の処理を行います
        /// </summary>
        /// <param name="selfParam">自身のパラメータ</param>
        /// <param name="selfTmpParam">自身の一時パラメータ</param>
        override protected void DetermineDestinationAndTargetOutOfAttackRange( in CharacterParameters ownerParams, in int[] ownerTileCosts )
        {
            // 最大評価ルート保存用
            (List<WaypointInformation> path, float evaluateValue) maxEvaluateRoute = (null, float.MinValue);

            // 未編集の現在のステージデータを用いる
            Func<TileDynamicData[]> setup = () =>
            {
                return _stageDataProvider.CurrentData.DeepCloneStageDynamicData();
            };
            // 移動値を無視した上で、進行可能なタイル全てをルート候補とする条件
            Func<TileDynamicData, object[], bool> condition = ( tileDData, args ) =>
            {
                var flag = TileBitFlag.CANNOT_MOVE;
                return ( !Methods.CheckBitFlag( tileDData.Flag, flag ) );
            };

            _owner.ActionRangeCtrl.SetupMoveableRangeDataFilterByCondition( setup, condition );

            // 各プレイヤーが存在するグリッドの評価値を計算する
            foreach ( Character chara in _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable( CHARACTER_TAG.PLAYER, CHARACTER_TAG.OTHER ) )
            {
                int destGridIndex       = chara.Params.TmpParam.GetCurrentGridIndex();
                ref float evaluateValue = ref _gridEvaluationValues[destGridIndex];

                evaluateValue += CalcurateEvaluateAttack( ownerParams.CharacterParam, chara.Params.CharacterParam );  // 攻撃による評価値を加算

                // 経路コストの逆数を乗算(経路コストが低い、つまりターゲットが存在するタイルの中でも、近ければ近いものほど評価値を大きくするため)
                if( !_owner.ActionRangeCtrl.FindMovePath( ownerParams.TmpParam.gridIndex, destGridIndex, ownerParams.CharacterParam.jumpForce, in ownerTileCosts ) )
                {
                    Debug.LogError("ルートの探索に失敗しました。出発インデックスや目的インデックスなどの設定を見直してください。");
                    continue;
                }
                var path        = _owner.ActionRangeCtrl.MovePathHdlr.ProposedMovePath.ToList();
                int totalCost   = path[^1].MoveCost;    // ^1は最後の要素のインデックス(C#8.0以降から使用可能)
                evaluateValue   *= 1f / totalCost;

                // 最も評価の高いルートを保存
                if ( maxEvaluateRoute.evaluateValue < evaluateValue ) { maxEvaluateRoute = (path, evaluateValue); }
            }

            // 得られたルートのパスをキャラクターの移動レンジ分に調整する
            _owner.ActionRangeCtrl.MovePathHdlr.AdjustPathToRangeAndSet( ownerParams.CharacterParam.moveRange, ownerParams.CharacterParam.jumpForce, in maxEvaluateRoute.path );
            _destinationTileIndex = _owner.ActionRangeCtrl.MovePathHdlr.ProposedMovePath[^1].TileIndex;
        }
    }
}