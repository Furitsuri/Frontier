using System;
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
        None        = -1,
        Keyboard,
        GamePad,
        TouchPanel,
    }

    // 入力元デバイス
    private InputSource _inputSource          = InputSource.None;
    // 入力インターフェース
    private IInput _iInput                      = null;
    // 入力ガイド表示
    private InputGuidePresenter _inputGuideView = null;
    // InputFacade内のInputCodeの参照値
    private ReadOnlyCollection<InputCode> _inputCodes;
    // ガイドアイコン毎に対応した入力関数
    private IGetInputBase[] _inputForIcons;

    /// <summary>
    /// 初期化します
    /// </summary>
    /// <param name="inputGuidePresenter">入力ガイド表示クラス</param>
    /// <param name="inputCodes">入力情報コード</param>
    public void Init(InputGuidePresenter inputGuidePresenter, InputCode[] inputCodes)
    {
        _inputGuideView = inputGuidePresenter;
        _inputCodes     = Array.AsReadOnly(inputCodes);

        InitializeInputSource();
        InitializeInputForGuideIcon();
    }

    /// <summary>
    /// 入力デバイスを切り替えます
    /// </summary>
    /// <param name="iInput">切替先の入力デバイス</param>
    private void SwitchInputDevice(IInput iInput)
    {
        _iInput = iInput;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateInputDevice();
        UpdateInputCodes();
    }

    /// <summary>
    /// ガイドアイコン毎に対応する入力関数を初期化します
    /// </summary>
    private void InitializeInputForGuideIcon()
    {
        _inputForIcons                                              = new IGetInputBase[(int)Constants.GuideIcon.NUM_MAX];
        _inputForIcons[(int)Constants.GuideIcon.ALL_CURSOR]         = new GetDirectionalInput(_iInput.GetDirectionalPressed);
        _inputForIcons[(int)Constants.GuideIcon.VERTICAL_CURSOR]    = new GetDirectionalInput(_iInput.GetDirectionalPressed);
        _inputForIcons[(int)Constants.GuideIcon.HORIZONTAL_CURSOR]  = new GetDirectionalInput(_iInput.GetDirectionalPressed);
        _inputForIcons[(int)Constants.GuideIcon.CONFIRM]            = new GetBooleanInput(_iInput.IsConfirmPressed);
        _inputForIcons[(int)Constants.GuideIcon.CANCEL]             = new GetBooleanInput(_iInput.IsCancelPressed);
        _inputForIcons[(int)Constants.GuideIcon.TOOL]               = new GetBooleanInput(_iInput.IsToolPressed);
        _inputForIcons[(int)Constants.GuideIcon.INFO]               = new GetBooleanInput(_iInput.IsInfoPressed);
        _inputForIcons[(int)Constants.GuideIcon.OPT1]               = new GetBooleanInput(_iInput.IsOptions1Pressed);
        _inputForIcons[(int)Constants.GuideIcon.OPT2]               = new GetBooleanInput(_iInput.IsOptions2Pressed);
        _inputForIcons[(int)Constants.GuideIcon.SUB1]               = new GetBooleanInput(_iInput.IsSub1Pressed);
        _inputForIcons[(int)Constants.GuideIcon.SUB2]               = new GetBooleanInput(_iInput.IsSub2Pressed);
        _inputForIcons[(int)Constants.GuideIcon.SUB3]               = new GetBooleanInput(_iInput.IsSub3Pressed);
        _inputForIcons[(int)Constants.GuideIcon.SUB4]               = new GetBooleanInput(_iInput.IsSub4Pressed);
#if UNITY_EDITOR
        _inputForIcons[(int)Constants.GuideIcon.DEBUG_MENU]         = new GetBooleanInput(_iInput.IsDebugMenuPressed);
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
            SwitchInputDevice(_iInput);
        }
        else if ( _inputSource != InputSource.GamePad &&
            Gamepad.current != null &&
            Gamepad.current.buttonSouth.wasPressedThisFrame )
        {
            _inputSource = InputSource.GamePad;
            _iInput = new PadInput();
            SwitchInputDevice(_iInput);
        }
        else if( _inputSource != InputSource.TouchPanel &&
            Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.isPressed )
        {
            _inputSource = InputSource.TouchPanel;
            _iInput= new TouchInput();
            SwitchInputDevice(_iInput);
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
            bool enable = ( code.EnableCb != null && code.EnableCb() );

            if (!enable || !code.IsIntervalTimePassed()) continue;

            var input = _inputForIcons[(int)code.Icon].GetInput();
            code.ExecuteAcceptInputCallback(input);
        }
    }
}