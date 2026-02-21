using System;
using UnityEngine;

public class InputContext
{
    private bool[] _buttons = new bool[Enum.GetValues( typeof( GameButton ) ).Length];

    public Vector2 Stick;
    public Direction Cursor;

    public void SetButton( GameButton button, bool value )
    {
        _buttons[( int ) button] = value;
    }

    public bool GetButton( GameButton button )
    {
        return _buttons[( int ) button];
    }
}