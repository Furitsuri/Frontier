using Frontier.Battle;
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
        /// <summary>
        /// 自身の攻撃(移動)可能範囲内に存在する攻撃対象キャラクターの情報です
        /// </summary>
        public struct TargetCandidateInfo
        {
            public int gridIndex;
            public List<int> targetCharaIndexs;
        }

        [Inject] protected BattleRoutineController _btlRtnCtrl      = null;
        [Inject] protected HierarchyBuilderBase _hierarchyBld       = null;
        [Inject] protected StageController _stageCtrl               = null;
        [Inject] protected IStageDataProvider _stageDataProvider    = null;
        
        protected int _destinationTileIndex         = -1;                               // 移動目標グリッドのインデックス値
        protected float[] _gridEvaluationValues     = null;                             // 各グリッドの評価値
        protected bool _isDetermined                = false;                            // 既に移動対象や攻撃対象を決定しているか
        protected Character _owner                  = null;
        protected Character _targetCharacter        = null;                             // 攻撃対象のキャラクター
        protected List<TargetCandidateInfo> _targetChandidateInfos  = null;             // 攻撃(移動)可能範囲内に存在する攻撃対象キャラクター

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
        /// 指定インデックスの十字方向にいる敵対キャラクターのキャラクターインデックスを抽出します
        /// </summary>
        /// <param name="baseIndex">指定インデックス(十字方向の中心インデックス)</param>
        /// <param name="opponentCharaIndexs">抜き出しに使用するリスト</param>
        protected void ExtractAttackabkeOpponentIndexs( int baseIndex, CHARACTER_TAG ownerTag, out List<CharacterKey> opponentCharaIndexs )
        {
            (int TileRowNum, int TileColNum) = _stageCtrl.GetGridNumsXZ();

            // 十字方向の判定関数とインデックスをタプルに詰め込む
            (Func<bool> lambda, int index)[] tuples = new (Func<bool>, int)[]
            {
                (() => baseIndex % TileColNum != 0,                       baseIndex - 1),
                (() => (baseIndex + 1) % TileColNum != 0,                 baseIndex + 1),
                (() => 0 <= (baseIndex - TileColNum),                     baseIndex - TileColNum),
                (() => (baseIndex + TileColNum) < _stageDataProvider.CurrentData.GetTileTotalNum(), baseIndex + TileColNum)
            };

            opponentCharaIndexs = new List<CharacterKey>( tuples.Length );

            foreach( var tuple in tuples )
            {
                if( tuple.lambda() )
                {
                    // 敵対勢力(攻撃可能)であることを確認した上でリストに追加
                    var tileData = _stageCtrl.GetTileDynamicData( tuple.index );
                    if( BattleLogicBase.IsOpponentFaction[Convert.ToInt32( ownerTag )]( tileData.CharaKey.CharacterTag ) )
                    {
                        opponentCharaIndexs.Add( tileData.CharaKey );
                    }
                }
            }
        }

        /// <summary>
        /// 対象のキャラクターを攻撃した際の評価値を計算します
        /// </summary>
        /// <param name="mySelf">自身</param>
        /// <param name="TargetCharacter">対象のキャラクター</param>
        /// <returns>評価値</returns>
        protected float CalcurateEvaluateAttack( in Status selfParam, in Status targetParam )
        {
            float evaluateValue = 0f;

            // 与ダメージをそのまま評価値にして使用
            evaluateValue = Mathf.Max( 0, selfParam.Atk - targetParam.Def );

            // 倒すことが出来る場合はボーナスを加算
            if ( targetParam.CurHP <= evaluateValue ) evaluateValue += ENABLE_DEFEAT_VALUE;

            return evaluateValue;
        }

        /// <summary>
        /// 進行予定の移動ルートを取得する際、自身の攻撃範囲に攻撃可能キャラクターが居た場合の処理を行います
        /// </summary>
        /// <param name="selfParam">自身のパラメータ</param>
        /// <param name="selfTmpParam">自身の一時パラメータ</param>
        virtual protected void DetermineDestinationAndTargetInAttackRange( in BattleParameters battleParams, in int[] ownerTileCosts, List<(int gridIndex, List<CharacterKey> opponents)> candidates ) { }

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
            _targetChandidateInfos  = new List<TargetCandidateInfo>(64);
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
    }
}