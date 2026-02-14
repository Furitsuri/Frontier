using System;
using System.Collections.ObjectModel;
using System.Linq;
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
    public void Init( InputGuidePresenter inputGuidePresenter, InputCode[] inputCodes )
    {
        _inputGuideView = inputGuidePresenter;
        _inputCodes = Array.AsReadOnly( inputCodes );

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
        _inputForIcons[( int ) GuideIcon.ALL_CURSOR]        = new GetDirectionalInput( _iInput.GetDirectionalPressed );
        _inputForIcons[( int ) GuideIcon.VERTICAL_CURSOR]   = new GetDirectionalInput( _iInput.GetDirectionalPressed );
        _inputForIcons[( int ) GuideIcon.HORIZONTAL_CURSOR] = new GetDirectionalInput( _iInput.GetDirectionalPressed );
        _inputForIcons[( int ) GuideIcon.CONFIRM]           = new GetBooleanInput( _iInput.IsConfirmPressed );
        _inputForIcons[( int ) GuideIcon.CANCEL]            = new GetBooleanInput( _iInput.IsCancelPressed );
        _inputForIcons[( int ) GuideIcon.TOOL]              = new GetBooleanInput( _iInput.IsToolPressed );
        _inputForIcons[( int ) GuideIcon.INFO]              = new GetBooleanInput( _iInput.IsInfoPressed );
        _inputForIcons[( int ) GuideIcon.OPT1]              = new GetBooleanInput( _iInput.IsOptions1Pressed );
        _inputForIcons[( int ) GuideIcon.OPT2]              = new GetBooleanInput( _iInput.IsOptions2Pressed );
        _inputForIcons[( int ) GuideIcon.SUB1]              = new GetBooleanInput( _iInput.IsSub1Pressed );
        _inputForIcons[( int ) GuideIcon.SUB2]              = new GetBooleanInput( _iInput.IsSub2Pressed );
        _inputForIcons[( int ) GuideIcon.SUB3]              = new GetBooleanInput( _iInput.IsSub3Pressed );
        _inputForIcons[( int ) GuideIcon.SUB4]              = new GetBooleanInput( _iInput.IsSub4Pressed );
        _inputForIcons[( int ) GuideIcon.CAMERA_MOVE]       = new GetVectorInput( _iInput.GetVectorPressed );
#if UNITY_EDITOR
        _inputForIcons[( int ) GuideIcon.DEBUG_MENU]        = new GetBooleanInput( _iInput.IsDebugMenuPressed );
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

            for( int i = 0; i < code.Icons.Length; ++i )
            {
                bool enable = ( code.EnableCbs[i] != null && code.EnableCbs[i]() );
                if( !enable ) { continue; }

                var input = _inputForIcons[( int ) code.Icons[i]].GetInput();
                if( code.ExecuteAcceptInputCallback( input, i ) ) { break; }   // 入力があった場合は必ずブレークする
            }
        }
    }
}