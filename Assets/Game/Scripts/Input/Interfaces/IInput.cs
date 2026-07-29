using UnityEngine;

/// <summary>
/// 入力インターフェ―スです
/// キーボードやゲームパッドなど、入力デバイスごとに切り替えます
/// </summary>
public interface IInput
{
    public Direction GetDirectionalPress();
    public bool IsConfirmPressed( InputTriggerMode mode );
    public bool IsCancelPressed( InputTriggerMode mode );
    public bool IsToolPressed( InputTriggerMode mode );
    public bool IsInfoPressed( InputTriggerMode mode );
    public bool IsOptions1Pressed( InputTriggerMode mode );
    public bool IsOptions2Pressed( InputTriggerMode mode );
    public bool IsSub1Pressed( InputTriggerMode mode );
    public bool IsSub2Pressed( InputTriggerMode mode );
    public bool IsSub3Pressed( InputTriggerMode mode );
    public bool IsSub4Pressed( InputTriggerMode mode );
    public bool IsPointerLeftPress( InputTriggerMode mode );
    public bool IsPointerRightPress( InputTriggerMode mode );
    public bool IsPointerMiddlePress( InputTriggerMode mode );
    public Vector2 GetVectorPressed();
    public bool IsDebugMenuPressed( InputTriggerMode mode );
}