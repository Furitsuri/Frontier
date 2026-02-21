using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    /// <summary>
    /// 入力元のデバイス
    /// </summary>
    private enum InputSource
    {
        None = -1,
        Keyboard,
        GamePad,
        TouchPanel,
    }

    private InputSource _inputSource = InputSource.None;    // 入力元デバイス
    private IInput _iInput = null;                          // 入力インターフェース
    private IGetInputBase[] _inputForIcons;                 // ガイドアイコン毎に対応した入力関数
    private InputGuidePresenter _inputGuideView = null;     // 入力ガイド表示
    private ReadOnlyCollection<InputCode> _inputCodes;      // InputFacade内のInputCodeの参照値

    public void Setup()
    {
    }

    /// <summary>
    /// 初期化します
    /// </summary>
    /// <param name="inputGuidePresenter">入力ガイド表示クラス</param>
    /// <param name="inputCodes">入力情報コード</param>
    public void Init( InputGuidePresenter inputGuidePresenter, List<InputCode> inputCodes )
    {
        _inputGuideView = inputGuidePresenter;
        _inputCodes = inputCodes.AsReadOnly();

        InitializeInputSource();
        InitializeInputForGuideIcon();
    }

    /// <summary>
    /// 入力デバイスを切り替えます
    /// </summary>
    /// <param name="iInput">切替先の入力デバイス</param>
    private void SwitchInputDevice( IInput iInput )
    {
        _iInput = iInput;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateInputDevice();
        UpdateInputCodes();

        _inputGuideView.Update();
    }

    /// <summary>
    /// ガイドアイコン毎に対応する入力関数を初期化します
    /// </summary>
    private void InitializeInputForGuideIcon()
    {
        _inputForIcons = new IGetInputBase[( int ) GuideIcon.NUM_MAX];
        _inputForIcons[( int ) GuideIcon.ALL_CURSOR]        = new GetDirectionalInput( _iInput.GetDirectionalPress );
        _inputForIcons[( int ) GuideIcon.VERTICAL_CURSOR]   = new GetDirectionalInput( _iInput.GetDirectionalPress );
        _inputForIcons[( int ) GuideIcon.HORIZONTAL_CURSOR] = new GetDirectionalInput( _iInput.GetDirectionalPress );
        _inputForIcons[( int ) GuideIcon.CONFIRM]           = new GetBooleanInput( _iInput.IsConfirmPressed, GameButton.Confirm );
        _inputForIcons[( int ) GuideIcon.CANCEL]            = new GetBooleanInput( _iInput.IsCancelPressed, GameButton.Cancel );
        _inputForIcons[( int ) GuideIcon.TOOL]              = new GetBooleanInput( _iInput.IsToolPressed, GameButton.Tool );
        _inputForIcons[( int ) GuideIcon.INFO]              = new GetBooleanInput( _iInput.IsInfoPressed, GameButton.Info );
        _inputForIcons[( int ) GuideIcon.OPT1]              = new GetBooleanInput( _iInput.IsOptions1Pressed, GameButton.Opt1 );
        _inputForIcons[( int ) GuideIcon.OPT2]              = new GetBooleanInput( _iInput.IsOptions2Pressed, GameButton.Opt2 );
        _inputForIcons[( int ) GuideIcon.SUB1]              = new GetBooleanInput( _iInput.IsSub1Pressed, GameButton.Sub1 );
        _inputForIcons[( int ) GuideIcon.SUB2]              = new GetBooleanInput( _iInput.IsSub2Pressed, GameButton.Sub2 );
        _inputForIcons[( int ) GuideIcon.SUB3]              = new GetBooleanInput( _iInput.IsSub3Pressed, GameButton.Sub3 );
        _inputForIcons[( int ) GuideIcon.SUB4]              = new GetBooleanInput( _iInput.IsSub4Pressed, GameButton.Sub4 );
        _inputForIcons[( int ) GuideIcon.POINTER_MOVE]      = new GetVectorInput( _iInput.GetVectorPressed );
        _inputForIcons[( int ) GuideIcon.POINTER_LEFT]      = new GetBooleanInput( _iInput.IsPointerLeftPress, GameButton.PointerLeft );
        _inputForIcons[( int ) GuideIcon.POINTER_RIGHT]     = new GetBooleanInput( _iInput.IsPointerRightPress, GameButton.PointerRight );
        _inputForIcons[( int ) GuideIcon.POINTER_MIDDLE]    = new GetBooleanInput( _iInput.IsPointerMiddlePress, GameButton.PointerMiddle );
#if UNITY_EDITOR
        _inputForIcons[( int ) GuideIcon.DEBUG_MENU]        = new GetBooleanInput( _iInput.IsDebugMenuPressed, GameButton.Debug );
#endif  // UNITY_EDITOR
    }

    /// <summary>
    /// 入力対応するデバイス設定を初期化します
    /// </summary>
    private void InitializeInputSource()
    {
        if( Keyboard.current != null )
        {
            _inputSource = InputSource.Keyboard;
            _iInput = new KeyboardInput();
        }
        else if( Gamepad.current != null )
        {
            _inputSource = InputSource.GamePad;
            _iInput = new PadInput();
        }
        else if( Touchscreen.current != null )
        {
            _inputSource = InputSource.TouchPanel;
        }
        else
        {
            _inputSource = InputSource.None;
        }

        if( _iInput != null )
        {
            SwitchInputDevice( _iInput );
        }
    }

    /// <summary>
    /// 入力を検知して、使用するデバイスの設定を切り替えます
    /// </summary>
    private void UpdateInputDevice()
    {
        if( _inputSource != InputSource.Keyboard &&
            Keyboard.current != null &&
            Keyboard.current.anyKey.wasPressedThisFrame )
        {
            _inputSource = InputSource.Keyboard;
            _iInput = new KeyboardInput();
            SwitchInputDevice( _iInput );
        }
        else if( _inputSource != InputSource.GamePad &&
            Gamepad.current != null &&
            Gamepad.current.buttonSouth.wasPressedThisFrame )
        {
            _inputSource = InputSource.GamePad;
            _iInput = new PadInput();
            SwitchInputDevice( _iInput );
        }
        else if( _inputSource != InputSource.TouchPanel &&
            Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.isPressed )
        {
            _inputSource = InputSource.TouchPanel;
            _iInput = new TouchInput();
            SwitchInputDevice( _iInput );
        }
    }

    /// <summary>
    /// 入力情報の判定・走査を更新します
    /// </summary>
    private void UpdateInputCodes()
    {
        // コールバック関数が設定されている場合は動作させる
        foreach( var code in _inputCodes )
        {
            // 有効判定コールバックが登録されていない or インターバル時間が過ぎていない場合は無効
            if( code.EnableCbs == null || !code.IsIntervalTimePassed() ) { continue; }

            // 同時入力設定の場合は先頭のガイドアイコンに対応する入力可否関数、及び入力受付関数を参照する
            if( code.IsSimultaneousInput )
            {
                if( UpdateSimultaneousInput( code ) ) { break; }
            }
            // 単一入力設定
            else {
                if( UpdateSingleInput( code ) ) { break; }
            }
        }
    }

    private bool UpdateSingleInput( InputCode code )
    {
        for( int i = 0; i < code.Icons.Length; ++i )
        {
            bool enable = ( code.EnableCbs[i] != null && code.EnableCbs[i]() );
            if( !enable ) { continue; }

            InputContext inputContext = new InputContext();
            _inputForIcons[( int ) code.Icons[i]].Apply( inputContext );
            if( code.ExecuteAcceptInputCallback( inputContext, i ) ) { return true; }   // 入力があった場合は必ずブレークする
        }

        return false;
    }

    private bool UpdateSimultaneousInput( InputCode code )
    {
        if( !code.EnableCbs[0]() ) { return false; }

        int iconNum = code.Icons.Length;
        InputContext inputContext = new InputContext();

        for( int i = 0; i < iconNum; ++i )
        {
            _inputForIcons[( int ) code.Icons[i]].Apply( inputContext );
        }
        if( code.ExecuteAcceptSimultaneousInputCallback( inputContext ) ) { return true; }

        return false;
    }
}