using Frontier;
using System;
using UnityEngine;
using System.Linq;
using Zenject;
using static Constants;
using Cysharp.Threading.Tasks;

public class InputFacade
{
    private HierarchyBuilderBase _hierarchyBld      = null;
    private InputGuidePresenter _inputGuideView = null;
    private IUiSystem _uiSystem                  = null;
    private InputHandler _inputHdl              = null;
    private InputCode[] _inputCodes;
#if UNITY_EDITOR
    private InputCode _debugTransitionInputCode = null;
#endif // UNITY_EDITOR


    [Inject]
    public void Construct( HierarchyBuilderBase hierarchyBld, IUiSystem uiSystem )
    {
        _hierarchyBld   = hierarchyBld;
        _uiSystem       = uiSystem;
    }

    /// <summary>
    /// 初期化します
    /// </summary>
    public void Init()
    {
        // 入力コード情報を全て初期化
        InitInputCodes();

        if (_inputHdl == null)
        {
            _inputHdl = _hierarchyBld.CreateComponentAndOrganize<InputHandler>(true, "InputHandler");
            NullCheck.AssertNotNull( _inputHdl );
        }

        if( _inputGuideView == null )
        {
            _inputGuideView = _uiSystem.GeneralUi.InputGuideView;
            NullCheck.AssertNotNull(_inputGuideView);
        }

        // 入力コード情報を受け渡す
        _inputHdl.Init(_inputGuideView, _inputCodes);
        _inputGuideView.Init(_inputCodes);
    }

    /// <summary>
    /// 判定対象となる入力コードを初期化します
    /// Iconは変更しないため、そのままにします
    /// </summary>
    public void UnregisterInputCodes()
    {
        for ( int i = 0; i < (int)Constants.GuideIcon.NUM_MAX; ++i)
        {
            _inputCodes[i].Explanation      = "";
            _inputCodes[i].EnableCb         = null;
            _inputCodes[i].ResetIntervalTime();
            _inputCodes[i].SetInputLastTime(0.0f);
        }
    }

    /// <summary>
    /// 登録済みの入力コードはそのままに、
    /// 現在のゲーム遷移において有効とする操作入力を画面上に表示するガイドUIと併せて登録します。
    /// また、そのキーを押下した際の処理をコールバックとして登録します。
    /// </summary>
    /// <param name="args">登録するアイコン、その説明文、及び押下時に対応する処理の関数コールバック</param>
    public void RegisterInputCodes( params InputCode[] args )
    {
#if UNITY_EDITOR
        // デバッグメニューに遷移するための入力コードをコピーさせる形で登録
        if (_debugTransitionInputCode != null)
        {   
            InputCode.CopyInputCode(_debugTransitionInputCode, _inputCodes[(int)Constants.GuideIcon.DEBUG_MENU]);
        }
#endif // UNITY_EDITOR

        foreach ( var arg in args )
        {
            // _inputCodesが未登録であれば登録する
            if (_inputCodes[(int)arg.Icon].IsUnRegistererd())
            {
                _inputCodes[(int)arg.Icon] = arg;
            }
            else
            {
                LogHelper.LogError($"InputCode is already registered. Icon: {arg.Icon}, Explanation: {arg.Explanation}");
            }
        }

        // ガイドアイコンを登録
        _inputGuideView.RegisterInputGuides();
    }

#if UNITY_EDITOR
    public void RegisterInputCodesInDebug(params InputCode[] args)
    {
        foreach (var arg in args)
        {
            // _inputCodesが未登録であれば登録する
            if (_inputCodes[(int)arg.Icon].IsUnRegistererd())
            {
                _inputCodes[(int)arg.Icon] = arg;
            }
            else
            {
                LogHelper.LogError($"InputCode is already registered. Icon: {arg.Icon}, Explanation: {arg.Explanation}");
            }
        }

        // ガイドアイコンを登録
        _inputGuideView.RegisterInputGuides();
    }

    public void RegisterInputCodeForDebugTransition( InputCode debugTransitionInputCode )
    {
        _debugTransitionInputCode = debugTransitionInputCode;
    }
#endif // UNITY_EDITOR

    /// <summary>
    /// 登録している入力コードを初期化した上で、
    /// 現在のゲーム遷移において有効とする操作入力を画面上に表示するガイドUIと併せて登録します。
    /// また、そのキーを押下した際の処理をコールバックとして登録します。
    /// </summary>
    /// <param name="args">登録するアイコン、その説明文、及び押下時に対応する処理の関数コールバック</param>
    public void ReRegisterInputCodes<T>(InputCode[] args)
    {
        UnregisterInputCodes();

        RegisterInputCodes( args );
    }

    /// <summary>
    /// 判定対象となる入力コードを初期化します
    /// </summary>
    private void InitInputCodes()
    {
        _inputCodes = new InputCode[(int)Constants.GuideIcon.NUM_MAX]
        {
            ( Constants.GuideIcon.ALL_CURSOR,          "", null, null, 0.0f ),
            ( Constants.GuideIcon.VERTICAL_CURSOR,     "", null, null, 0.0f ),
            ( Constants.GuideIcon.HORIZONTAL_CURSOR,   "", null, null, 0.0f ),
            ( Constants.GuideIcon.CONFIRM,             "", null, null, 0.0f ),
            ( Constants.GuideIcon.CANCEL,              "", null, null, 0.0f ),
            ( Constants.GuideIcon.TOOL,                "", null, null, 0.0f ),
            ( Constants.GuideIcon.INFO,                "", null, null, 0.0f ),
            ( Constants.GuideIcon.SPACE,              "", null, null, 0.0f ),
            ( Constants.GuideIcon.SUB1,                "", null, null, 0.0f ),
            ( Constants.GuideIcon.SUB2,                "", null, null, 0.0f ),
            ( Constants.GuideIcon.SUB3,                "", null, null, 0.0f ),
            ( Constants.GuideIcon.SUB4,                "", null, null, 0.0f ),
#if UNITY_EDITOR
            ( Constants.GuideIcon.DEBUG_MENU,          "", null, null, 0.0f )
#endif  // UNITY_EDITOR
        };
    }

    /// <summary>
    /// 入力コードのインターバル時間をリセットします。
    /// </summary>
    private void ResetIntervalTimeOnInputCodes()
    {
        for (int i = 0; i < _inputCodes.Length; ++i)
        {
            _inputCodes[i].ResetIntervalTime();
        }
    }
}