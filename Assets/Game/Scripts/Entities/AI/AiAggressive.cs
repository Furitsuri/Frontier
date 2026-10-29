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
        protected override float ATTACKABLE_VALUE { get; } = 50;
        protected override float WITHIN_RANGE_VALUE { get; } = 50;

        // 攻撃の候補の評価に用いる値
        // 撃破 > (撃破出来ない場合)ダメージ割合 > コスト の順に優先されるよう、各値の大きさに十分な差を設けている
        private const float DEFEAT_VALUE            = 1000f;    // 攻撃対象1体を撃破出来る場合の加算値
        private const float DAMAGE_RATIO_VALUE      = 100f;     // ダメージ割合(残りHPに対する与ダメージの割合。最大100%)が100%の場合の加算値
        private const float ACTION_GAUGE_COST_VALUE = 1f;       // 消費するアクションゲージ1あたりの減算値

        /// <summary>
        /// 各グリッドの評価値を計算します
        /// </summary>
        /// <param name="selfParam">自身のパラメータ</param>
        /// <param name="selfTmpParam">自身の一時パラメータ</param>
        /// <returns>有効となる目的地及び攻撃対象がそれぞれ設定されたか否か</returns>
        public override (bool, bool) DetermineDestinationAndTarget( BattleParameters battleParams, in Status status, in int[] ownerTileCosts, in CharacterKey ownerKey )
        {
            _isDetermined = true;

            // 自身の行動範囲を計算して表示する
            // MEMO : 敵の移動時(EmMoveState)は、ここで表示した行動範囲がそのまま表示されることを前提としています。
            //        また、移動経路の探索(FindMovePath)はここで計算した移動可能範囲を用います
            float curHeight = _stageCtrl.GetTileStaticData( battleParams.TmpParam.CurrentTileIndex ).Height;
            _owner.BattleLogic.ActionRangeCtrl.SetupActionableRangeData( battleParams.TmpParam.CurrentTileIndex, curHeight );
            _owner.BattleLogic.ActionRangeCtrl.DrawActionableRange();

            // スキルを含めた攻撃の候補が存在する場合は、その中から攻撃を選択する
            var options = _attackOptionCollector.Collect( _owner );
            if( 0 < options.Count )
            {
                DetermineDestinationAndTargetInAttackRange( in battleParams, in ownerTileCosts, options );
            }
            // 攻撃範囲内に敵対キャラクターが存在しない場合は、評価値を計算して最も高いグリッド位置へ向かうように
            else
            {
                DetermineDestinationAndTargetOutOfAttackRange( in battleParams, in ownerTileCosts );
            }

            ResetAllTileEvaluationValues(); // 必ず評価値をリセットしてから終了する

            return (IsValidDestination(), IsValidTarget());
        }

        /// <summary>
        /// 攻撃の候補を評価します。
        /// 攻撃対象1体ごとに、撃破出来る場合は撃破の評価を、そうでない場合はダメージ割合の評価を加算し(範囲攻撃の場合は合計)、
        /// 消費するアクションゲージに応じて減算します。
        /// これにより、通常攻撃で撃破出来る相手 > スキルを使用すれば撃破出来る相手 > ダメージ割合が最も大きい攻撃 の順に選択されます
        /// MEMO : 攻撃によって自身に付与される状態効果(AttackForecast.SelfStatusEffects)は、仕組みが未実装のため評価していません
        /// </summary>
        /// <param name="option">評価する攻撃の候補</param>
        /// <returns>評価値</returns>
        protected override float EvaluateAttackOption( in AttackOption option )
        {
            float evaluateValue = 0f;
            int actionGaugeCost = 0;

            foreach( var key in option.AttackTargetKeys )
            {
                var target = _btlRtnCtrl.BtlCharaCdr.GetCharacter( key );
                if( target == null ) { continue; }

                var forecast    = AttackForecaster.Forecast( _owner, target, option.SkillID );
                actionGaugeCost = forecast.ActionGaugeCost;

                if( forecast.IsDefeat )
                {
                    evaluateValue += DEFEAT_VALUE;
                }
                else
                {
                    // 残りHPが0以下の相手は撃破済みとして扱われるため、ここでは残りHPが正の値であることが前提
                    float damageRatio = Mathf.Min( ( float ) forecast.TotalDamage / forecast.TargetCurrentHp, 1f );
                    evaluateValue += damageRatio * DAMAGE_RATIO_VALUE;
                }
            }

            evaluateValue -= actionGaugeCost * ACTION_GAUGE_COST_VALUE;

            return evaluateValue;
        }

        /// <summary>
        /// 進行予定の移動ルートを取得する際、自身の攻撃範囲に攻撃可能キャラクターが居た場合の処理を行います
        /// </summary>
        /// <param name="battleParams">自身の戦闘パラメータ</param>
        /// <param name="ownerTileCosts">自身のタイル移動コスト</param>
        /// <param name="options">攻撃の候補</param>
        protected override void DetermineDestinationAndTargetInAttackRange( in BattleParameters battleParams, in int[] ownerTileCosts, List<AttackOption> options )
        {
            // 評価が最も高い攻撃を求める
            if( !TrySelectAttackOption( options, out var selected ) ) { return; }

            // 選択した攻撃を行う位置と主となる攻撃対象を、目標移動位置、攻撃対象に設定
            SetAttackOption( in selected );

            _owner.BattleLogic.ActionRangeCtrl.MovePathHdlr.FindMovePath( battleParams.TmpParam.CurrentTileIndex, _destinationTileIndex, _owner.GetStatusRef.jumpForce, ownerTileCosts, _owner.BattleLogic.ActionRangeCtrl.ActionableTileData.MoveableTileMap );
        }

        /// <summary>
        /// 進行予定の移動ルートを取得する際、自身の攻撃範囲に攻撃可能キャラクターが居ない場合の処理を行います
        /// </summary>
        /// <param name="selfParam">自身のパラメータ</param>
        /// <param name="selfTmpParam">自身の一時パラメータ</param>
        protected override void DetermineDestinationAndTargetOutOfAttackRange( in BattleParameters battleParams, in int[] ownerTileCosts )
        {
            // 最大評価ルート保存用
            (List<WaypointInformation> path, float evaluateValue) maxEvaluateRoute = (null, float.MinValue);

            // 未編集の現在のステージデータを用いる
            Func<TileDynamicData[]> setup = () =>
            {
                return _stageDataProvider.CurrentData.DeepCloneStageDynamicData();
            };
            // 移動値を無視した上で、進行可能なタイル全てをルート候補とする条件
            Func<TileDynamicData, object[], bool> condition = ( tileDynamicData, args ) =>
            {
                var flag = TileBitFlag.CANNOT_MOVE;
                return ( !Methods.HasAnyFlag( tileDynamicData.Flag, flag ) );
            };

            _owner.BattleLogic.ActionRangeCtrl.SetupMoveableRangeDataFilterByCondition( setup, condition );

            // 各プレイヤーが存在するグリッドの評価値を計算する
            foreach ( Character chara in _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable( CHARACTER_TAG.PLAYER, CHARACTER_TAG.OTHER ) )
            {
                int destGridIndex       = chara.BattleParams.TmpParam.CurrentTileIndex;
                ref float evaluateValue = ref _gridEvaluationValues[destGridIndex];

                evaluateValue += CalcurateEvaluateAttack( chara );  // 攻撃による評価値を加算

                // 経路コストの逆数を乗算(経路コストが低い、つまりターゲットが存在するタイルの中でも、近ければ近いものほど評価値を大きくするため)
                // 高低差等で到達出来ない対象は候補から外す
                if( !_owner.BattleLogic.ActionRangeCtrl.FindMovePath( battleParams.TmpParam.CurrentTileIndex, destGridIndex, _owner.GetStatusRef.jumpForce, in ownerTileCosts ) )
                {
                    continue;
                }
                var path        = _owner.BattleLogic.ActionRangeCtrl.MovePathHdlr.ProposedMovePath.ToList();
                int totalCost   = path[^1].MoveCost;    // ^1は最後の要素のインデックス(C#8.0以降から使用可能)
                evaluateValue   *= 1f / totalCost;

                // 最も評価の高いルートを保存
                if ( maxEvaluateRoute.evaluateValue < evaluateValue ) { maxEvaluateRoute = (path, evaluateValue); }
            }

            // 得られたルートのパスをキャラクターの移動レンジ分に調整する
            _owner.BattleLogic.ActionRangeCtrl.MovePathHdlr.AdjustPathToRangeAndSet( _owner.GetStatusRef.moveRange, _owner.GetStatusRef.jumpForce, in maxEvaluateRoute.path );
            var proposedPath = _owner.BattleLogic.ActionRangeCtrl.MovePathHdlr.ProposedMovePath;
            if( 0 < proposedPath.Count )
            {
                _destinationTileIndex = proposedPath[^1].TileIndex;
            }
        }
    }
}