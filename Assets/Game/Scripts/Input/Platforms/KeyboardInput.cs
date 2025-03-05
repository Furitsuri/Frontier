using UnityEngine;

/// <summary>
/// キーボード入力における各入力判定を管理します
/// </summary>
public class KeyboardInput : IInput
{
    public Constants.Direction GetDirectionalPressed()
    {

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) return Constants.Direction.FORWARD;
        else if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) return Constants.Direction.LEFT;
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) return Constants.Direction.BACK;
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) return Constants.Direction.RIGHT;

        return Constants.Direction.NONE;
    }

    public bool IsConfirmPressed()
    { 
        return Input.GetKeyUp( KeyCode.Space );
    }

    public bool IsCancelPressed()
    { 
        return Input.GetKeyUp( KeyCode.Backspace );
    }

    public bool IsPausePressed()
    { 
        return false;
    }

    public bool IsOptionsPressed()
    { 
        return Input.GetKeyUp( KeyCode.Escape );
    }
}