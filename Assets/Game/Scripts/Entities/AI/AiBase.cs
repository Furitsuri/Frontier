using Frontier.Battle;
using Frontier.Combat;
using Frontier.Entities;
using Frontier.Stage;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Frontier.Entities.Ai
{
    public class AiBase : BaseAi
    {
        [Inject] protected BattleRoutineController _btlRtnCtrl      = null;
        [Inject] protected HierarchyBuilderBase _hierarchyBld       = null;
        [Inject] protected StageController _stageCtrl               = null;
        [Inject] protected IStageDataProvider _stageDataProvider    = null;

        protected int _destinationTileIndex         = -1;                               // 移動目標グリッドのインデックス値
        protected float[] _gridEvaluationValues     = null;                             // 各グリッドの評価値
        protected bool _isDetermined                = false;                            // 既に移動対象や攻撃対象を決定しているか
        protected Character _owner                  = null;
        protected Character _targetCharacter        = null;                             // 攻撃対象のキャラクター
        protected AttackOptionCollector _attackOptionCollector = null;                  // 攻撃の候補の列挙
        protected AttackOption _attackOption;                                           // 選択した攻撃(_isAttackOptionSelectedがtrueの場合のみ有効)
        protected bool _isAttackOptionSelected      = false;                            // 攻撃を選択しているか
        protected IReadOnlyDictionary<int, float> _contestedTileWeights = null;        // 他の味方が攻撃位置として必要としているタイルと、その必要度

        // 他の味方が攻撃位置として必要としているタイルに立つ攻撃の候補への減算値(必要度1あたり)。
        // 撃破の可否やダメージの大小は覆さず、評価がほぼ等しい候補どうしであれば味方の攻撃位置を空ける程度の大きさとする
        // (AiAggressiveの評価ではダメージ割合20%分に相当)
        protected const float CONTESTED_TILE_PENALTY = 20f;

        virtual protected float ATTACKABLE_VALUE { get; } = 0;
        virtual protected float WITHIN_RANGE_VALUE { get; } = 0;
        virtual protected float ENABLE_DEFEAT_VALUE { get; } = 0;

        /// <summary>
        /// 移動目標が有効かを判定します
        /// </summary>
        /// <returns>有効か否か</returns>
        public bool IsValidDestination()
        {
            return 0 <= _destinationTileIndex;
        }

        /// <summary>
        /// 攻撃対象が有効かを判定します
        /// </summary>
        /// <returns>有効か否か</returns>
        public bool IsValidTarget()
        {
            return _targetCharacter != null;
        }

        /// <summary>
        /// 攻撃対象キャラクターインデックス値が有効かを判定します
        /// </summary>
        /// <returns>有効か否か</returns>
        public bool IsValidTargetCharacterIndex()
        {
            return ( _targetCharacter != null && _targetCharacter.GetStatusRef.characterTag != CHARACTER_TAG.ENEMY );
        }

        /// <summary>
        /// 全てのタイルの評価値をリセットします
        /// </summary>
        protected void ResetAllTileEvaluationValues()
        {
            for( int i = 0; i < _gridEvaluationValues.Length; ++i )
            {
                _gridEvaluationValues[i] = 0f;
            }
        }

        /// <summary>
        /// 選択した攻撃を設定します。攻撃を行う位置を移動目標に、主となる攻撃対象を攻撃対象に設定します
        /// </summary>
        /// <param name="option">選択した攻撃</param>
        protected void SetAttackOption( in AttackOption option )
        {
            _attackOption           = option;
            _isAttackOptionSelected = true;
            _destinationTileIndex   = option.StandTileIndex;
            _targetCharacter        = option.MainTarget;
        }

        /// <summary>
        /// 対象のキャラクターを攻撃した際の評価値を計算します
        /// </summary>
        /// <param name="target">対象のキャラクター</param>
        /// <returns>評価値</returns>
        protected float CalcurateEvaluateAttack( Character target )
        {
            var forecast = AttackForecaster.Forecast( _owner, target );

            // 与ダメージをそのまま評価値にして使用
            float evaluateValue = forecast.TotalDamage;

            // 倒すことが出来る場合はボーナスを加算
            if( forecast.IsDefeat ) evaluateValue += ENABLE_DEFEAT_VALUE;

            return evaluateValue;
        }

        /// <summary>
        /// 攻撃の候補を評価します。評価値が高いほど優先して選択されます。
        /// 評価の方針は思考タイプ毎に異なるため、派生クラスでオーバーライドしてください
        /// </summary>
        /// <param name="option">評価する攻撃の候補</param>
        /// <returns>評価値</returns>
        virtual protected float EvaluateAttackOption( in AttackOption option ) { return 0f; }

        /// <summary>
        /// 攻撃の候補が、他の味方が攻撃位置として必要としているタイルに立つ(居座る)ものである場合の減算値を求めます。
        /// 攻撃後に立っている位置(移動を伴うスキルの場合は着地先)で判定します
        /// </summary>
        /// <param name="option">評価する攻撃の候補</param>
        /// <returns>減算値</returns>
        protected float EvaluateContestedTilePenalty( in AttackOption option )
        {
            if( _contestedTileWeights == null ) { return 0f; }

            int finalTileIndex = ( 0 <= option.LandingTileIndex ) ? option.LandingTileIndex : option.StandTileIndex;

            return _contestedTileWeights.TryGetValue( finalTileIndex, out float weight ) ? weight * CONTESTED_TILE_PENALTY : 0f;
        }

        /// <summary>
        /// 他の味方が攻撃位置として必要としているタイルと、その必要度を設定します
        /// </summary>
        public override void SetContestedTiles( IReadOnlyDictionary<int, float> weights )
        {
            _contestedTileWeights = weights;
        }

        /// <summary>
        /// 攻撃の候補の中から、評価値が最も高いものを選択します。
        /// 評価値が等しい場合は消費するアクションゲージが少ないものを、それも等しい場合は先に列挙されたものを優先します
        /// </summary>
        /// <param name="options">攻撃の候補</param>
        /// <param name="selected">選択された攻撃の候補</param>
        /// <returns>選択出来たか否か(候補が存在しない場合はfalse)</returns>
        protected bool TrySelectAttackOption( List<AttackOption> options, out AttackOption selected )
        {
            selected = default;
            if( options == null || options.Count <= 0 ) { return false; }

            float maxValue  = float.MinValue;
            int minCost     = int.MaxValue;

            foreach( var option in options )
            {
                float value = EvaluateAttackOption( in option ) - EvaluateContestedTilePenalty( in option );
                int cost    = SkillsData.IsValidSkill( option.SkillID ) ? SkillsData.data[( int ) option.SkillID].Cost : 0;

                if( maxValue < value || ( maxValue == value && cost < minCost ) )
                {
                    maxValue    = value;
                    minCost     = cost;
                    selected    = option;
                }
            }

            return true;
        }

        /// <summary>
        /// 進行予定の移動ルートを取得する際、自身の攻撃範囲に攻撃可能キャラクターが居た場合の処理を行います
        /// </summary>
        /// <param name="battleParams">自身の戦闘パラメータ</param>
        /// <param name="ownerTileCosts">自身のタイル移動コスト</param>
        /// <param name="options">攻撃の候補</param>
        virtual protected void DetermineDestinationAndTargetInAttackRange( in BattleParameters battleParams, in int[] ownerTileCosts, List<AttackOption> options ) { }

        /// <summary>
        /// 進行予定の移動ルートを取得する際、自身の攻撃範囲に攻撃可能キャラクターが居ない場合の処理を行います
        /// </summary>
        /// <param name="selfParam">自身のパラメータ</param>
        /// <param name="selfTmpParam">自身の一時パラメータ</param>
        virtual protected void DetermineDestinationAndTargetOutOfAttackRange( in BattleParameters battleParams, in int[] ownerTileCosts ) { }

        /// <summary>
        /// 初期化します
        /// </summary>
        public override void Init( Character owner )
        {
            _owner                  = owner;
            _gridEvaluationValues   = new float[_stageDataProvider.CurrentData.GetTileTotalNum()];

            LazyInject.GetOrCreate( ref _attackOptionCollector, () => _hierarchyBld.InstantiateWithDiContainer<AttackOptionCollector>( false ) );
        }

        /// <summary>
        /// 移動目標と攻撃対象キャラクターをリセットします
        /// TODO : 再行動スキルなどを実装する場合は、対象に再行動を適応した際にこの関数を呼び出してください
        /// </summary>
        public override void ResetDestinationAndTarget()
        {
            _isDetermined           = false;
            _destinationTileIndex   = -1;
            _targetCharacter        = null;
            _attackOption           = default;
            _isAttackOptionSelected = false;
            _contestedTileWeights   = null;
        }

        /// <summary>
        /// 予兆を伴うスキルによる攻撃の候補の中から、評価が最も高いものを選択します。
        /// 他の味方の攻撃位置を塞ぐかどうかは考慮しません(予約した攻撃はその場で行うため、立ち位置を選ぶ余地が無いため)
        /// </summary>
        public override bool TrySelectTelegraphOption( List<AttackOption> options, out AttackOption selected )
        {
            var contestedTileWeights = _contestedTileWeights;
            _contestedTileWeights    = null;
            bool isSelected          = TrySelectAttackOption( options, out selected );
            _contestedTileWeights    = contestedTileWeights;

            return isSelected;
        }

        /// <summary>
        /// 予約していた攻撃を、このフェーズの行動として決定済みにします
        /// </summary>
        public override void ApplyReservedAttackOption( in AttackOption option )
        {
            ResetDestinationAndTarget();
            SetAttackOption( in option );
            _destinationTileIndex   = -1;   // その場で攻撃を行うため、移動先は持たない
            _isDetermined           = true;
        }

        /// <summary>
        /// 既に移動対象や攻撃対象を決定しているかどうかの情報を取得します
        /// </summary>
        /// <returns>決定の有無</returns>
        public override bool IsDetermined() { return _isDetermined; }

        /// <summary>
        /// 目的地のグリッドインデックスを取得します
        /// </summary>
        /// <returns>目的地のグリッドインデックス</returns>
        public override int GetDestinationGridIndex()
        {
            return _destinationTileIndex;
        }

        /// <summary>
        /// 攻撃対象のキャラクターを取得します
        /// </summary>
        /// <returns>攻撃対象のキャラクター</returns>
        public override Character GetTargetCharacter()
        {
            return _targetCharacter;
        }

        /// <summary>
        /// 選択した攻撃を取得します
        /// </summary>
        /// <param name="option">選択した攻撃</param>
        /// <returns>攻撃を選択しているか否か</returns>
        public override bool TryGetAttackOption( out AttackOption option )
        {
            option = _attackOption;
            return _isAttackOptionSelected;
        }
    }
}