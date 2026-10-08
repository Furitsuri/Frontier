using System;
using UnityEngine;

public class InputContext
{
    private bool[] _buttons = new bool[Enum.GetValues( typeof( GameButton ) ).Length];

    public Vector2 Stick;
    public Direction Cursor;
    // 離した状態から押された(または押し直された)最初のフレームの入力か否か。
    // 押しっぱなし継続中の入力と区別したい受付側(1回の押下につき1度だけ反応させたい処理等)が参照する
    public bool IsNewPress;

    public void SetButton( GameButton button, bool value )
    {
        _buttons[( int ) button] = value;
    }

    public bool GetButton( GameButton button )
    {
        return _buttons[( int ) button];
    }
}