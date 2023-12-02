using System;

/// <summary>
/// このクラスをインスタンスに持つオブジェクトのタイムスケールを管理します
/// </summary>
public class TimeScale
{
    private float _timeScale = 1f;
    public float CurrentTimeScale => _timeScale;

    public Action<float> OnValueChange;

    public void SetTimeScale(float timeScale)
    {
        _timeScale = timeScale;
        OnValueChange?.Invoke(timeScale);
    }
}