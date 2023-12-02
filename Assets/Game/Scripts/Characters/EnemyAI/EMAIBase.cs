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
        /// 自身の攻撃(移動)可能範囲内に存在する攻撃対象キャラクターの情報です
        /// </summary>
        public struct TargetCandidateInfo
        {
            public int gridIndex;
            public List<int> targetCharaIndexs;
        }

        protected BattleManager _btlMgr;
        protected StageController _stageCtrl;
        // 既に移動対象や攻撃対象を決定しているか
        protected bool _isDetermined = false;
        // 移動目標グリッドのインデックス値
        protected int _destinationGridIndex = -1;
        // 攻撃対象のキャラクターのインデックス値
        protected Character _targetCharacter = null;
        // 各グリッドの評価値
        protected float[] _gridEvaluationValues = null;
        // 攻撃(移動)可能範囲内に存在する攻撃対象キャラクター
        protected List<TargetCandidateInfo> _targetChandidateInfos = null;
        // 進行経路
        protected List<(int routeIndex, int routeCost)> _suggestedMoveRoute;

        virtual protected float ATTACKABLE_TARGET_VALUE { get; } = 0;
        virtual protected float WITHIN_RANGE_VALUE { get; } = 0;
        virtual protected float ENABLE_DEFEAT_VALUE { get; } = 0;

        /// <summary>
        /// 初期化します
        /// </summary>
        virtual public void Init(Enemy mySelf, BattleManager btlMgr, StageController stgCtrl)
        {
            _btlMgr = btlMgr;
            _stageCtrl = stgCtrl;
            _gridEvaluationValues = new float[_stageCtrl.GridTotalNum];
            _targetChandidateInfos = new List<TargetCandidateInfo>(64);
        }

        /// <summary>
        /// 目的地のグリッドインデックスを取得します
        /// </summary>
        /// <returns>目的地のグリッドインデックス</returns>
        public int GetDestinationGridIndex()
        {
            return _destinationGridIndex;
        }

        /// <summary>
        /// 攻撃対象のキャラクターを取得します
        /// </summary>
        /// <returns>攻撃対象のキャラクター</returns>
        public Character GetTargetCharacter()
        {
            return _targetCharacter;
        }

        /// <summary>
        /// 進行予定の移動ルートを取得します
        /// </summary>
        /// <returns>進行予定の移動ルート情報</returns>
        public List<(int routeIndex, int routeCost)> GetProposedMoveRoute()
        {
            return _suggestedMoveRoute;
        }

        /// <summary>
        /// 移動目標と攻撃対象キャラクターをリセットします
        /// TODO : 再行動スキルなどを実装する場合は、対象に再行動を適応した際にこの関数を呼び出してください
        /// </summary>
        public void ResetDestinationAndTarget()
        {
            _isDetermined = false;
            _destinationGridIndex = -1;
            _targetCharacter = null;
        }

        /// <summary>
        /// 既に移動対象や攻撃対象を決定しているかどうかの情報を取得します
        /// </summary>
        /// <returns>決定の有無</returns>
        public bool IsDetermined() { return _isDetermined; }

        /// <summary>
        /// 移動目標が有効かを判定します
        /// </summary>
        /// <returns>有効か否か</returns>
        public bool IsValidDestination()
        {
            return 0 <= _destinationGridIndex;
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
            return (_targetCharacter != null && _targetCharacter.param.characterTag != Character.CHARACTER_TAG.ENEMY);
        }

        /// <summary>
        /// 対象のキャラクターを攻撃した際の評価値を計算します
        /// </summary>
        /// <param name="mySelf">自身</param>
        /// <param name="TargetCharacter">対象のキャラクター</param>
        /// <returns>評価値</returns>
        protected float CalcurateEvaluateAttack(in Character.Parameter selfParam, in Character.Parameter targetParam)
        {
            float evaluateValue = 0f;

            // 与ダメージをそのまま評価値にして使用
            evaluateValue = Mathf.Max(0, selfParam.Atk - targetParam.Def);

            // 倒すことが出来る場合はボーナスを加算
            if (targetParam.CurHP <= evaluateValue) evaluateValue += ENABLE_DEFEAT_VALUE;

            return evaluateValue;
        }

        /// <summary>
        /// 指定インデックスの十字方向にいる敵対キャラクターのキャラクターインデックスを抽出します
        /// </summary>
        /// <param name="baseIndex">指定インデックス(十字方向の中心インデックス)</param>
        /// <param name="opponentCharaIndexs">抜き出しに使用するリスト</param>
        protected void ExtractAttackabkeOpponentIndexs(int baseIndex, out List<CharacterHashtable.Key> opponentCharaIndexs)
        {
            opponentCharaIndexs = new List<CharacterHashtable.Key>(4);
;
            (int GridRowNum, int GridColumnNum) = _stageCtrl.GetGridNumsXZ();

            // 十字方向の判定関数とインデックスをタプルに詰め込む
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
        /// いずれかのターゲットに攻撃可能なグリッドの評価値を返します
        /// </summary>
        /// <param name="info">指定グリッド情報</param>
        /// <returns>評価値</returns>
        virtual protected float GetEvaluateEnableTargetAttackBase(in Stage.GridInfo info) { return ATTACKABLE_TARGET_VALUE; }

        virtual protected float GetEvaluateEnableDefeat(in Stage.GridInfo info) { return ENABLE_DEFEAT_VALUE; }
    }
}