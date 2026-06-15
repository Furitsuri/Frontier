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

    public bool IsConfirmPressed()
    {
        return Input.GetKeyUp( KeyCode.Return );
    }

    public bool IsCancelPressed()
    {
        return Input.GetKeyUp( KeyCode.Escape );
    }

    public bool IsToolPressed()
    {
        return Input.GetKeyUp( KeyCode.F );
    }

    public bool IsInfoPressed()
    {
        return Input.GetKeyUp( KeyCode.R );
    }

    public bool IsOptions1Pressed()
    {
        return Input.GetKeyUp( KeyCode.P );
    }

    public bool IsOptions2Pressed()
    {
        return Input.GetKeyUp( KeyCode.Tab );
    }

    public bool IsSub1Pressed()
    {
        return Input.GetKeyUp( KeyCode.Q );
    }

    public bool IsSub2Pressed()
    {
        return Input.GetKeyUp( KeyCode.E );
    }

    public bool IsSub3Pressed()
    {
        return Input.GetKeyUp( KeyCode.Z );
    }

    public bool IsSub4Pressed()
    {
        return Input.GetKeyUp( KeyCode.C );
    }

    public bool IsPointerLeftPress()
    {
        return Input.GetMouseButton( INPUT_INDEX_MOUSE_LEFT_CLICK );
    }
    public bool IsPointerRightPress()
    {
        return Input.GetMouseButton( INPUT_INDEX_MOUSE_RIGHT_CLICK );
    }
    public bool IsPointerMiddlePress()
    {
        return Input.GetMouseButton( INPUT_INDEX_MOUSE_MIDDLE_CLICK );
    }

    public Vector2 GetVectorPressed()
    {
        if( !Input.GetMouseButton( INPUT_INDEX_MOUSE_RIGHT_CLICK ) ) { return Vector2.zero; }

        float mouseX = Input.GetAxis( "Mouse X" );
        float mouseY = Input.GetAxis( "Mouse Y" );

        return new Vector2( mouseX, mouseY );
    }

    public bool IsDebugMenuPressed()
    {
        return Input.GetKeyUp( KeyCode.F12 );
    }
}