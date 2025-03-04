using System;
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
    // 前フレームで入力が有効であったかの確認
    private bool[] _prevEnableCbs               = null;
    // 最後に入力操作をした時間の保持
    private float _operateInputLastTime         = 0.0f;
    // InputFacade内のInputCodeの参照値(書き換え不可)
    private ReadOnlyCollection<InputFacade.ToggleInputCode> _refInputCodes;

    // Start is called before the first frame update
    void Start()
    {
        InitializeTargetDevice();
        _prevEnableCbs = new bool[(int)Constants.GuideIcon.NUM_MAX];
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
        foreach (var code in _refInputCodes)
        {
            int codeIdx = (int)code.Icon;
            bool enable = code.EnableCb != null && code.EnableCb();

            if (enable)
            {
                if (code.InputCb != null)
                {
                    code.InputCb();
                }
            }
        }
    }

    /// <summary>
    /// ユーザーがキー操作を行った際に、
    /// 短い時間で何度も同じキーが押下されたと判定されないためにインターバル時間を設けます
    /// </summary>
    /// <returns>キー操作が有効か無効か</returns>
    private bool OperateInputControl()
    {
        if (Constants.OPERATE_KET_INTERVAL <= Time.time - _operateInputLastTime)
        {
            _operateInputLastTime = Time.time;

            return true;
        }

        return false;
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
    public void Init( InputGuidePresenter inputGuidePresenter, InputFacade.ToggleInputCode[] inputCodes )
    {
        _inputGuideView    = inputGuidePresenter;
        _refInputCodes     = Array.AsReadOnly( inputCodes );
    }

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