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

    // Start is called before the first frame update
    void Start()
    {
        InitializeInputSource();
        InitializeInputForGuideIcon();
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
        GetDirectionalInput.GetDirectionalInputCallback directionalInput  = _iInput.GetDirectionalPressed;
        GetBooleanInput.GetBooleanInputCallback confirmInput              = _iInput.IsConfirmPressed;
        GetBooleanInput.GetBooleanInputCallback cancelInput               = _iInput.IsCancelPressed;
        GetBooleanInput.GetBooleanInputCallback optionalInput             = _iInput.IsOptionsPressed;

        _inputForIcons                                              = new IGetInputBase[(int)Constants.GuideIcon.NUM_MAX];
        _inputForIcons[(int)Constants.GuideIcon.ALL_CURSOR]         = new GetDirectionalInput(directionalInput);
        _inputForIcons[(int)Constants.GuideIcon.VERTICAL_CURSOR]    = new GetDirectionalInput(directionalInput);
        _inputForIcons[(int)Constants.GuideIcon.HORIZONTAL_CURSOR]  = new GetDirectionalInput(directionalInput);
        _inputForIcons[(int)Constants.GuideIcon.CONFIRM]            = new GetBooleanInput(confirmInput);
        _inputForIcons[(int)Constants.GuideIcon.CANCEL]             = new GetBooleanInput(cancelInput);
        _inputForIcons[(int)Constants.GuideIcon.ESCAPE]             = new GetBooleanInput(optionalInput);
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

    /// <summary>
    /// 入力デバイスを切り替えます
    /// </summary>
    /// <param name="iInput">切替先の入力デバイス</param>
    private void SwitchInputDevice( IInput iInput )
    {
        _iInput = iInput;
    }

    /// <summary>
    /// 初期化します
    /// </summary>
    /// <param name="inputGuidePresenter">入力ガイド表示クラス</param>
    /// <param name="inputCodes">入力情報コード</param>
    public void Init( InputGuidePresenter inputGuidePresenter, InputCode[] inputCodes )
    {
        _inputGuideView = inputGuidePresenter;
        _inputCodes     = Array.AsReadOnly( inputCodes );
    }

    /// <summary>
    /// 押下された方向入力の種類を取得します
    /// </summary>
    /// <returns>押下されたボタンに対応する方向</returns>
    public Constants.Direction GetDirectionalPressed() { return _iInput.GetDirectionalPressed(); }

    /// <summary>
    /// 決定入力が押下されたかを取得します
    /// </summary>
    /// <returns>ボタンの押下</returns>
    public bool IsConfirmPressed() { return _iInput.IsConfirmPressed(); }

    /// <summary>
    /// 取消入力が押下されたかを取得します
    /// </summary>
    /// <returns>ボタンの押下</returns>
    public bool IsCancelPressed() { return _iInput.IsCancelPressed(); }

    /// <summary>
    /// オプション入力が押下されたかを取得します
    /// </summary>
    /// <returns>ボタンの押下</returns>
    public bool IsOptionsPressed() { return _iInput.IsOptionsPressed(); }
}