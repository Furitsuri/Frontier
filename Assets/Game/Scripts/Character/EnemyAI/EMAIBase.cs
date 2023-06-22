using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EMAIBase
{
    private Enemy _mySelf;
    // �e�O���b�h�̕]���l
    protected float[] _evaluationValueOfGrids = null;

    /// <summary>
    /// ���������܂�
    /// </summary>
    virtual public void Init( Enemy mySelf )
    {
        _mySelf = mySelf;
        _evaluationValueOfGrids = new float[StageGrid.Instance.GridTotalNum];
    }

    /// <summary>
    /// �S�ẴO���b�h�ɑ΂��Ă��ꂼ��̕]���l���쐬���܂�
    /// </summary>
    /// <param name="param">���g�̃p�����[�^</param>
    /// <param name="tmpParam">���g�̈ꎞ�ێ��p�����[�^</param>
    public void CreateEvaluationValues( in Character.Parameter param, in Character.TmpParameter tmpParam )
    {
        for( int i = 0; i < _evaluationValueOfGrids.Length; ++i )
        {
            _evaluationValueOfGrids[i] = CalcurateEvaluationValue(i, param, tmpParam);
        }
    }

    /// <summary>
    /// �w��̃O���b�h�̕]���l���v�Z���܂�
    /// </summary>
    /// <param name="gridIndex">�w�肷��O���b�h�̃C���f�b�N�X�l</param>
    /// <param name="param">���g�̃p�����[�^</param>
    /// <param name="tmpParam">���g�̈ꎞ�ێ��p�����[�^</param>
    /// <returns>�]���l</returns>
    virtual protected float CalcurateEvaluationValue( int gridIndex, in Character.Parameter param, in Character.TmpParameter tmpParam)
    {
        return 0f;
    }
}