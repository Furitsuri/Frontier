using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    /// <summary>
    /// 入力対応中のデバイス
    /// </summary>
    private enum TargetDevice
    {
        None        = -1,
        Keyboard,
        GamePad,
        TouchPanel,
    }

    private TargetDevice _targetDevice          = TargetDevice.None;
    // 入力インターフェース
    private IInput _iInput                      = null;
    // 入力ガイド表示
    private InputGuidePresenter _inputGuideView = null;
    // InputFacade内のInputCodeの参照値
    private ReadOnlyCollection<InputCode> _inputCodes;

    // Start is called before the first frame update
    void Start()
    {
        InitializeTargetDevice();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateInputDevice();
        UpdateInputCodes();
    }

    /// <summary>
    /// 入力対応するデバイス設定を初期化します
    /// </summary>
    private void InitializeTargetDevice()
    {
        if( Keyboard.current != null )
        {
            _targetDevice = TargetDevice.Keyboard;
            _iInput = new KeyboardInput();
        }
        else if( Gamepad.current != null )
        {
            _targetDevice = TargetDevice.GamePad;
            _iInput = new PadInput();
        }
        else if( Touchscreen.current != null )
        {
            _targetDevice = TargetDevice.TouchPanel;
        }
        else
        {
            _targetDevice = TargetDevice.None;
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
        if( _targetDevice != TargetDevice.Keyboard &&
            Keyboard.current != null &&
            Keyboard.current.anyKey.wasPressedThisFrame )
        {
            _targetDevice = TargetDevice.Keyboard;
            _iInput = new KeyboardInput();
            SwitchInputDevice(_iInput);
        }
        else if ( _targetDevice != TargetDevice.GamePad &&
            Gamepad.current != null &&
            Gamepad.current.buttonSouth.wasPressedThisFrame )
        {
            _targetDevice = TargetDevice.GamePad;
            _iInput = new PadInput();
            SwitchInputDevice(_iInput);
        }
        else if( _targetDevice != TargetDevice.TouchPanel &&
            Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.isPressed )
        {
            _targetDevice = TargetDevice.TouchPanel;
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
            bool enable = code.EnableCb != null && code.EnableCb();

            if (!enable || !code.IsIntervalTimePassed()) continue;
            
            if (code.InputCb == null) continue;

            if (code.InputCb())
            {
                code.SetInputLastTime(Time.time);
            }
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
        _inputGuideView     = inputGuidePresenter;
        // _inputCodes         = inputCodes;
        _inputCodes         = Array.AsReadOnly( inputCodes );
    }

    /// <summary>
    /// 押下された方向ボタンの種類を取得します
    /// </summary>
    /// <returns>押下されたボタンに対応する方向</returns>
    public Constants.Direction GetDirectionalPressed() { return _iInput.GetDirectionalPressed(); }

    /// <summary>
    /// 決定ボタンが押下されたかを取得します
    /// </summary>
    /// <returns>ボタンの押下</returns>
    public bool IsConfirmPressed() { return _iInput.IsConfirmPressed(); }

    /// <summary>
    /// 取消ボタンが押下されたかを取得します
    /// </summary>
    /// <returns>ボタンの押下</returns>
    public bool IsCancelPressed() { return _iInput.IsCancelPressed(); }

    /// <summary>
    /// オプションボタンが押下されたかを取得します
    /// </summary>
    /// <returns>ボタンの押下</returns>
    public bool IsOptionsPressed() { return _iInput.IsOptionsPressed(); }
}