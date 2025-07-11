using UnityEngine;

/// <summary>
/// キーボード入力における各入力判定を管理します
/// </summary>
public class KeyboardInput : IInput
{
    public Constants.Direction GetDirectionalPressed()
    {

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))           return Constants.Direction.FORWARD;
        else if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))    return Constants.Direction.LEFT;
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))    return Constants.Direction.BACK;
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))   return Constants.Direction.RIGHT;

        return Constants.Direction.NONE;
    }

    public bool IsConfirmPressed()
    {
        return Input.GetKeyUp(KeyCode.Tab);
    }

    public bool IsCancelPressed()
    {
        return Input.GetKeyUp(KeyCode.Escape);
    }

    public bool IsToolPressed()
    {
        return Input.GetKeyUp(KeyCode.LeftControl);
    }

    public bool IsInfoPressed()
    {
        return Input.GetKeyUp(KeyCode.LeftShift);
    }

    public bool IsOptionsPressed()
    {
        return Input.GetKeyUp(KeyCode.Space);
    }

    public bool IsSub1Pressed()
    {
        return Input.GetKeyUp(KeyCode.Alpha1);
    }

    public bool IsSub2Pressed()
    {
        return Input.GetKeyUp(KeyCode.Alpha2);
    }

    public bool IsSub3Pressed()
    {
        return Input.GetKeyUp(KeyCode.Alpha3);
    }

    public bool IsSub4Pressed()
    {
        return Input.GetKeyUp(KeyCode.Alpha4);
    }

    public bool IsDebugMenuPressed()
    {
        return Input.GetKeyUp(KeyCode.F12);
    }
}