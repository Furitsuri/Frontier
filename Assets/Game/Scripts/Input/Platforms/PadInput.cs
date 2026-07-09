using UnityEngine;

/// <summary>
/// キーボード入力における各入力判定を管理します
/// </summary>
public class PadInput : IInput
{
    public Direction GetDirectionalPress()
    {
        if( Input.GetButtonUp( "Vertical" ) ) return Direction.FORWARD;
        else if( Input.GetButtonUp( "Horizontal" ) ) return Direction.LEFT;
        else if( Input.GetButtonUp( "Vertical" ) ) return Direction.BACK;
        else if( Input.GetButtonUp( "Horizontal" ) ) return Direction.RIGHT;

        return Direction.NONE;
    }

    /// <summary>
    /// InputTriggerModeに応じてGetButtonDown(Down)/GetButton(DownRepeat)/GetButtonUp(Up)を出し分けます
    /// </summary>
    private static bool GetButtonByMode( string buttonName, InputTriggerMode mode )
    {
        return mode switch
        {
            InputTriggerMode.Down       => Input.GetButtonDown( buttonName ),
            InputTriggerMode.DownRepeat => Input.GetButton( buttonName ),
            _                           => Input.GetButtonUp( buttonName ),
        };
    }

    public bool IsConfirmPressed( InputTriggerMode mode )
    {
        return GetButtonByMode( "Jump", mode );
    }

    public bool IsCancelPressed( InputTriggerMode mode )
    {
        return GetButtonByMode( "Cancel", mode );
    }

    public bool IsToolPressed( InputTriggerMode mode )
    {
        return GetButtonByMode( "Jump", mode );
    }

    public bool IsInfoPressed( InputTriggerMode mode )
    {
        return GetButtonByMode( "Cancel", mode );
    }

    public bool IsOptions1Pressed( InputTriggerMode mode )
    {
        return GetButtonByMode( "Submit", mode );
    }

    public bool IsOptions2Pressed( InputTriggerMode mode )
    {
        return GetButtonByMode( "Submit", mode );
    }

    public bool IsSub1Pressed( InputTriggerMode mode )
    {
        return GetButtonByMode( "joystick button 4", mode );
    }

    public bool IsSub2Pressed( InputTriggerMode mode )
    {
        return GetButtonByMode( "joystick button 5", mode );
    }

    public bool IsSub3Pressed( InputTriggerMode mode )
    {
        return GetButtonByMode( "joystick button 6", mode );
    }

    public bool IsSub4Pressed( InputTriggerMode mode )
    {
        return GetButtonByMode( "joystick button 7", mode );
    }

    public Vector2 GetVectorPressed()
    {
        return Vector2.zero;
    }

    /// <summary>
    /// パッド操作におけるポインター入力(マウスクリックに該当)は常時ONとしています
    /// </summary>
    /// <returns></returns>
    public bool IsPointerLeftPress( InputTriggerMode mode )
    {
        return true;
    }

    public bool IsPointerRightPress( InputTriggerMode mode )
    {
        return true;
    }

    public bool IsPointerMiddlePress( InputTriggerMode mode )
    {
        return true;
    }

    public bool IsDebugMenuPressed( InputTriggerMode mode )
    {
        return GetButtonByMode( "joystick button 8", mode );
    }
}