using UnityEngine;
using UnityEngine.UI;

public class ColorFlicker<T> where T : IColorEditable 
{
    [SerializeField]
    T colorFlickObject;
    [Header("1ループの長さ(秒単位)")]
    [SerializeField]
    [Range(0.1f, 10.0f)]
    float duration = 1.0f;
    [Header("ループ開始時の色")]
    [SerializeField]
    Color32 startColor = new Color32(255, 255, 255, 255);
    //ループ終了(折り返し)時の色を0～255までの整数で指定。
    [Header("ループ終了時の色")]
    [SerializeField]
    Color32 endColor = new Color32(255, 255, 255, 64);

    private bool _enabled = false;
    private float _elapsedTime = 0f;

    public ColorFlicker(T target)
    {
        colorFlickObject = target;
    }

    public void Init()
    {
        _enabled = false;
    }

    public void UpdateFlick()
    {
        if (!_enabled) return;

        _elapsedTime += Time.deltaTime;
        colorFlickObject.Color = Color.Lerp(startColor, endColor, Mathf.PingPong(_elapsedTime / duration, 1.0f));
    }

    /// <summary>
    /// 有効・非有効設定を行います
    /// </summary>
    /// <param name="enabled">有効・非有効</param>
    public void setEnabled(bool enabled)
    {
        _enabled = enabled;
        _elapsedTime = 0f;
    }
}