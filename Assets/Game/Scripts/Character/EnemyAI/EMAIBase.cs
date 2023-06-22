using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EMAIBase
{
    private Enemy _mySelf;
    // 各グリッドの評価値
    protected float[] _evaluationValueOfGrids = null;

    /// <summary>
    /// 初期化します
    /// </summary>
    virtual public void Init( Enemy mySelf )
    {
        _mySelf = mySelf;
        _evaluationValueOfGrids = new float[StageGrid.Instance.GridTotalNum];
    }

    /// <summary>
    /// 全てのグリッドに対してそれぞれの評価値を作成します
    /// </summary>
    /// <param name="param">自身のパラメータ</param>
    /// <param name="tmpParam">自身の一時保持パラメータ</param>
    public void CreateEvaluationValues( in Character.Parameter param, in Character.TmpParameter tmpParam )
    {
        for( int i = 0; i < _evaluationValueOfGrids.Length; ++i )
        {
            _evaluationValueOfGrids[i] = CalcurateEvaluationValue(i, param, tmpParam);
        }
    }

    /// <summary>
    /// 指定のグリッドの評価値を計算します
    /// </summary>
    /// <param name="gridIndex">指定するグリッドのインデックス値</param>
    /// <param name="param">自身のパラメータ</param>
    /// <param name="tmpParam">自身の一時保持パラメータ</param>
    /// <returns>評価値</returns>
    virtual protected float CalcurateEvaluationValue( int gridIndex, in Character.Parameter param, in Character.TmpParameter tmpParam)
    {
        return 0f;
    }
}