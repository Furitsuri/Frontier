using UnityEngine;
using static Constants;

/// <summary>
/// キーボード入力における各入力判定を管理します
/// </summary>
public class KeyboardInput : IInput
{
    public Direction GetDirectionalPress()
    {

        if( Input.GetKey( KeyCode.W ) || Input.GetKey( KeyCode.UpArrow ) ) return Direction.FORWARD;
        else if( Input.GetKey( KeyCode.A ) || Input.GetKey( KeyCode.LeftArrow ) ) return Direction.LEFT;
        else if( Input.GetKey( KeyCode.S ) || Input.GetKey( KeyCode.DownArrow ) ) return Direction.BACK;
        else if( Input.GetKey( KeyCode.D ) || Input.GetKey( KeyCode.RightArrow ) ) return Direction.RIGHT;

        return Direction.NONE;
    }

    /// <summary>
    /// InputTriggerModeに応じてGetKeyDown(Down)/GetKey(DownRepeat)/GetKeyUp(Up)を出し分けます
    /// </summary>
    private static bool GetKeyByMode( KeyCode key, InputTriggerMode mode )
    {
        return mode switch
        {
            InputTriggerMode.Down       => Input.GetKeyDown( key ),
            InputTriggerMode.DownRepeat => Input.GetKey( key ),
            _                           => Input.GetKeyUp( key ),
        };
    }

    public bool IsConfirmPressed( InputTriggerMode mode )
    {
        return GetKeyByMode( KeyCode.Return, mode );
    }

    public bool IsCancelPressed( InputTriggerMode mode )
    {
        return GetKeyByMode( KeyCode.Escape, mode );
    }

    public bool IsToolPressed( InputTriggerMode mode )
    {
        return GetKeyByMode( KeyCode.F, mode );
    }

    public bool IsInfoPressed( InputTriggerMode mode )
    {
        return GetKeyByMode( KeyCode.R, mode );
    }

    public bool IsOptions1Pressed( InputTriggerMode mode )
    {
        return GetKeyByMode( KeyCode.P, mode );
    }

    public bool IsOptions2Pressed( InputTriggerMode mode )
    {
        return GetKeyByMode( KeyCode.Tab, mode );
    }

    public bool IsSub1Pressed( InputTriggerMode mode )
    {
        return GetKeyByMode( KeyCode.Q, mode );
    }

    public bool IsSub2Pressed( InputTriggerMode mode )
    {
        return GetKeyByMode( KeyCode.E, mode );
    }

    public bool IsSub3Pressed( InputTriggerMode mode )
    {
        return GetKeyByMode( KeyCode.Z, mode );
    }

    public bool IsSub4Pressed( InputTriggerMode mode )
    {
        return GetKeyByMode( KeyCode.C, mode );
    }

    public bool IsPointerLeftPress( InputTriggerMode mode )
    {
        return Input.GetMouseButton( INPUT_INDEX_MOUSE_LEFT_CLICK );
    }
    public bool IsPointerRightPress( InputTriggerMode mode )
    {
        return Input.GetMouseButton( INPUT_INDEX_MOUSE_RIGHT_CLICK );
    }
    public bool IsPointerMiddlePress( InputTriggerMode mode )
    {
        return Input.GetMouseButton( INPUT_INDEX_MOUSE_MIDDLE_CLICK );
    }

    /// <summary>
    /// マウスの移動量を返します。
    /// どのボタンと組み合わせて使うか(左ドラッグ/右ドラッグ等)は呼び出し側で context.GetButton(...) を見て判定してください。
    /// </summary>
    public Vector2 GetVectorPressed()
    {
        float mouseX = Input.GetAxis( "Mouse X" );
        float mouseY = Input.GetAxis( "Mouse Y" );

        return new Vector2( mouseX, mouseY );
    }

    public bool IsDebugMenuPressed( InputTriggerMode mode )
    {
        return GetKeyByMode( KeyCode.F12, mode );
    }
}