using UnityEngine;

/// <summary>
/// キーボード入力における各入力判定を管理します
/// </summary>
public class PadInput : IInput
{
    public Constants.Direction GetDirectionalPressed()
    {
        if (Input.GetButtonUp("Vertical"))          return Constants.Direction.FORWARD;
        else if (Input.GetButtonUp("Horizontal"))   return Constants.Direction.LEFT;
        else if (Input.GetButtonUp("Vertical"))     return Constants.Direction.BACK;
        else if (Input.GetButtonUp("Horizontal"))   return Constants.Direction.RIGHT;

        return Constants.Direction.NONE;
    }

    public bool IsConfirmPressed()
    {
        return Input.GetButtonUp("Jump");
    }

    public bool IsCancelPressed()
    {
        return Input.GetButtonUp("Cancel");
    }

    public bool IsPausePressed()
    {
        return false;
    }

    public bool IsOptionsPressed()
    {
        return Input.GetButtonUp("Submit");
    }
}