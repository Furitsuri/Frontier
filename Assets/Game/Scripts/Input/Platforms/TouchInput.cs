using UnityEngine;

/// <summary>
/// キーボード入力における各入力判定を管理します
/// </summary>
public class TouchInput : IInput
{
    public Direction GetDirectionalPressed()
    {
        return Direction.NONE;
    }

    public bool IsConfirmPressed()
    {
        return false;
    }

    public bool IsCancelPressed()
    {
        return false;
    }

    public bool IsPausePressed()
    {
        return false;
    }

    public bool IsToolPressed()
    {
        return false;
    }

    public bool IsInfoPressed()
    {
        return false;
    }

    public bool IsOptions1Pressed()
    {
        return false;
    }

    public bool IsOptions2Pressed()
    {
        return false;
    }

    public bool IsSub1Pressed()
    { 
        return false;
    }

    public bool IsSub2Pressed()
    {
        return false;
    }

    public bool IsSub3Pressed()
    {
        return false;
    }

    public bool IsSub4Pressed()
    {
        return false;
    }

    public Vector2 GetVectorPressed()
    {
        return Vector2.zero;
    }

    public bool IsDebugMenuPressed()
    {
        return false;
    }
}