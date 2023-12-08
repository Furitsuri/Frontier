using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ���̃N���X��TimeScale�N���X��o�^�����C���X�^���X�̃^�C���X�P�[���Ǘ����s���܂�
/// </summary>
public class BattleTimeScaleController
{
    private float _timeScale = 1.0f;
    private readonly List<TimeScale> _timeScaleList = new();

    /// <summary>
    /// _timeScale�̒l���ύX���ꂽ�ۂɎ����ŌĂяo����郁�\�b�h�ł�
    /// �o�^����Ă���S�ẴC���X�^���X�̃^�C���X�P�[�����w�肳�ꂽ_timeScale�l�ɕύX���܂�
    /// </summary>
    void Notify()
    {
        foreach( var instance in _timeScaleList)
        {
            instance.SetTimeScale(_timeScale);
        }
    }

    // �o�^
    public void Regist(TimeScale scale)
    {
        _timeScaleList.Add(scale);
    }

    // �o�^����
    public void Unregist(TimeScale scale)
    {
        _timeScaleList.Remove(scale);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="timeScale"></param>
    public void SetTimeScale( float timeScale )
    {
        _timeScale = timeScale;
        Notify();
    }
}