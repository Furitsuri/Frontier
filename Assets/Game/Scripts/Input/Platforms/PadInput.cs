using UnityEngine;

/// <summary>
/// キーボード入力における各入力判定を管理します
/// </summary>
public class PadInput : IInput
{
    public Direction GetDirectionalPressed()
    {
        if ( Input.GetButtonUp("Vertical"))          return Direction.FORWARD;
        else if (Input.GetButtonUp("Horizontal"))   return Direction.LEFT;
        else if (Input.GetButtonUp("Vertical"))     return Direction.BACK;
        else if (Input.GetButtonUp("Horizontal"))   return Direction.RIGHT;

        return Direction.NONE;
    }

    public bool IsConfirmPressed()
    {
        return Input.GetButtonUp("Jump");
    }

    public bool IsCancelPressed()
    {
        return Input.GetButtonUp("Cancel");
    }

    public bool IsToolPressed()
    {
        return Input.GetButtonUp("Jump");
    }

    public bool IsInfoPressed()
    {
        return Input.GetButtonUp("Cancel"); ;
    }

    public bool IsOptions1Pressed()
    {
        return Input.GetButtonUp("Submit");
    }

    public bool IsOptions2Pressed()
    {
        return Input.GetButtonUp("Submit");
    }

    public bool IsSub1Pressed()
    {
        return Input.GetButtonUp("joystick button 4");
    }

    public bool IsSub2Pressed()
    {
        return Input.GetButtonUp("joystick button 5");
    }

    public bool IsSub3Pressed()
    {
        return Input.GetButtonUp("joystick button 6");
    }

    public bool IsSub4Pressed()
    {
        return Input.GetButtonUp("joystick button 7");
    }

    public bool IsDebugMenuPressed()
    {
        return Input.GetButtonUp("joystick button 8");
    }
}