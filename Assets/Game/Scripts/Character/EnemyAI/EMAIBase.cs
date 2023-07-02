using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

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

    // 移動目標グリッドのインデックス値
    protected int _destinationGridIndex = -1;
    // 攻撃対象のキャラクターのインデックス値
    protected Character _targetCharacter = null;
    // 各グリッドの評価値
    protected float[] _gridEvaluationValues = null;
    // 攻撃(移動)可能範囲内に存在する攻撃対象キャラクター
    protected List<TargetCandidateInfo> _targetChandidateInfos = null;
    // 進行経路
    protected List<(int routeIndex, int routeCost)> _proposedMoveRoute;

    virtual protected float TARGET_ATTACK_BASE_VALUE { get; } = 0;
    virtual protected float WITHIN_RANGE_VALUE { get; } = 0;
    virtual protected float ENABLE_DEFEAT_VALUE { get; } = 0;

    /// <summary>
    /// 初期化します
    /// </summary>
    virtual public void Init( Enemy mySelf )
    {
        _gridEvaluationValues = new float[StageGrid.Instance.GridTotalNum];
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

    public List<(int routeIndex, int routeCost)> GetProposedMoveRoute()
    {
        return _proposedMoveRoute;
    }

    /// <summary>
    /// 移動目標と攻撃対象キャラクターをリセットします
    /// </summary>
    public void ResetDestinationAndTarget()
    {
        _destinationGridIndex   = -1;
        _targetCharacter        = null;
    }

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
        return (_targetCharacter != null && _targetCharacter.param.characterTag != Character.CHARACTER_TAG.CHARACTER_ENEMY);
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

        // 与ダメージをそのまま評価値をして使用
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
    protected void ExtractAttackabkeOpponentIndexs( int baseIndex, out List<CharacterHashtable.Key> opponentCharaIndexs )
    {
        opponentCharaIndexs = new List<CharacterHashtable.Key>(4);

        var stageGrid = StageGrid.Instance;
        (int gridNumX, int gridNumZ) = stageGrid.GetGridNumsXZ();

        // 十字方向の判定関数とインデックスをタプルに詰め込む
        (Func<bool> lambda, int index)[] tuples = new (Func<bool>, int )[]
        {
            (() => baseIndex % gridNumX != 0,                       baseIndex - 1),
            (() => (baseIndex + 1) % gridNumX != 0,                 baseIndex + 1),
            (() => 0 <= (baseIndex - gridNumX),                     baseIndex - gridNumX),
            (() => (baseIndex + gridNumX) < stageGrid.GridTotalNum, baseIndex + gridNumX)
        };

        foreach (var tuple in tuples)
        {
            if (tuple.lambda())
            {
                var gridInfo = stageGrid.GetGridInfo(tuple.index);
                if (gridInfo.characterTag == Character.CHARACTER_TAG.CHARACTER_PLAYER || gridInfo.characterTag == Character.CHARACTER_TAG.CHARACTER_OTHER)
                {
                    opponentCharaIndexs.Add(new CharacterHashtable.Key(gridInfo.characterTag, gridInfo.charaIndex));
                }
            }
        }
    }

    virtual public ( bool, bool ) DetermineDestinationAndTarget(in Character.Parameter selfParam, in Character.TmpParameter selfTmpParam)
    {
        return (false, false);
    }

    /// <summary>
    /// いずれかのターゲットに攻撃可能なグリッドの評価値を返します
    /// </summary>
    /// <param name="info">指定グリッド情報</param>
    /// <returns>評価値</returns>
    virtual protected float GetEvaluateEnableTargetAttackBase(in StageGrid.GridInfo info) { return TARGET_ATTACK_BASE_VALUE; }

    virtual protected float GetEvaluateEnableDefeat(in StageGrid.GridInfo info) { return ENABLE_DEFEAT_VALUE; }
}