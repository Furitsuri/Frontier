using UnityEngine;

/// <summary>
/// キーボード入力における各入力判定を管理します
/// </summary>
public class TouchInput : IInput
{
    public Direction GetDirectionalPress()
    {
        return Direction.NONE;
    }

    public bool IsConfirmPressed( InputTriggerMode mode )
    {
        return false;
    }

    public bool IsCancelPressed( InputTriggerMode mode )
    {
        return false;
    }

    public bool IsPausePressed()
    {
        return false;
    }

    public bool IsToolPressed( InputTriggerMode mode )
    {
        return false;
    }

    public bool IsInfoPressed( InputTriggerMode mode )
    {
        return false;
    }

    public bool IsOptions1Pressed( InputTriggerMode mode )
    {
        return false;
    }

    public bool IsOptions2Pressed( InputTriggerMode mode )
    {
        return false;
    }

    public bool IsSub1Pressed( InputTriggerMode mode )
    {
        return false;
    }

    public bool IsSub2Pressed( InputTriggerMode mode )
    {
        return false;
    }

    public bool IsSub3Pressed( InputTriggerMode mode )
    {
        return false;
    }

    public bool IsSub4Pressed( InputTriggerMode mode )
    {
        return false;
    }

    public Vector2 GetVectorPressed()
    {
        return Vector2.zero;
    }

    public bool IsPointerLeftPress( InputTriggerMode mode )
    {
        return false;
    }

    public bool IsPointerRightPress( InputTriggerMode mode )
    {
        return false;
    }

    public bool IsPointerMiddlePress( InputTriggerMode mode )
    {
        return false;
    }

    public bool IsDebugMenuPressed( InputTriggerMode mode )
    {
        return false;
    }
}