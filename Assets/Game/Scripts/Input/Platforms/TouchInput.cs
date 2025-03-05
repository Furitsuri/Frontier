﻿using UnityEngine;

/// <summary>
/// キーボード入力における各入力判定を管理します
/// </summary>
public class TouchInput : IInput
{
    public Constants.Direction GetDirectionalPressed()
    {
        return Constants.Direction.NONE;
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

    public bool IsOptionsPressed()
    {
        return false;
    }
}