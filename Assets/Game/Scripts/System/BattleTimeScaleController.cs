using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// このクラスにTimeScaleクラスを登録したインスタンスのタイムスケール管理を行います
/// </summary>
public class BattleTimeScaleController
{
    private float _timeScale = 1.0f;
    private readonly List<TimeScale> _timeScaleList = new();

    /// <summary>
    /// _timeScaleの値が変更された際に自動で呼び出されるメソッドです
    /// 登録されている全てのインスタンスのタイムスケールを指定された_timeScale値に変更します
    /// </summary>
    void Notify()
    {
        foreach( var instance in _timeScaleList)
        {
            instance.SetTimeScale(_timeScale);
        }
    }

    // 登録
    public void Regist(TimeScale scale)
    {
        _timeScaleList.Add(scale);
    }

    // 登録解除
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