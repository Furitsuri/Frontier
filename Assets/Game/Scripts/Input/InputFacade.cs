using Frontier;
using System;
using UnityEngine;
using System.Linq;
using Zenject;
using static Constants;

public class InputFacade
{
    private HierarchyBuilderBase _hierarchyBld  = null;
    private InputGuidePresenter _inputGuideView = null;
    private IUiSystem _uiSystem                 = null;
    private InputHandler _inputHdlr             = null;
    private InputCode[] _inputCodes;

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

        if (_inputHdlr == null)
        {
            _inputHdlr = _hierarchyBld.CreateComponentAndOrganize<InputHandler>(true, "InputHandler");
            NullCheck.AssertNotNull( _inputHdlr );
        }

        if( _inputGuideView == null )
        {
            _inputGuideView = _uiSystem.GeneralUi.InputGuideView;
            NullCheck.AssertNotNull(_inputGuideView);
        }

        // 入力コード情報を受け渡す
        _inputHdlr.Init(_inputGuideView, _inputCodes);
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
    /// 指定するハッシュ値と一致する登録済みの入力コードを解除します。
    /// </summary>
    /// <param name="hashCode">ハッシュ値</param>
    public void UnregisterInputCodes( int hashCode )
    {
        for ( int i = 0; i < (int)Constants.GuideIcon.NUM_MAX; ++i)
        {
            if (_inputCodes[i].RegisterClassHashCode == hashCode)
            {
                _inputCodes[i].Explanation      = "";
                _inputCodes[i].EnableCb         = null;
                _inputCodes[i].ResetIntervalTime();
                _inputCodes[i].SetInputLastTime(0.0f);
            }
        }
    }

    /// <summary>
    /// 既に登録済みの入力コードはそのままに、
    /// 現在のゲーム遷移において有効にしたい入力コードを、画面上に表示するガイドUIと併せて登録します。
    /// また、そのキーを押下した際の処理をコールバックとして登録します。
    /// </summary>
    /// <param name="args">登録するアイコン、その説明文、及び押下時に対応する処理の関数コールバック</param>
    public void RegisterInputCodes( params InputCode[] args )
    {
        foreach ( var arg in args )
        {
            // _inputCodesが未登録であれば登録する
            // 1つのコードに複数アイコンを登録する場合は、先頭に指定しているアイコンを基準にする
            if (_inputCodes[(int)arg.Icons.First()].IsUnRegistererd())
            {
                _inputCodes[(int)arg.Icons.First()] = arg;
            }
            else
            {
                LogHelper.LogError($"InputCode is already registered. Icon: {arg.Icons.First()}, Explanation: {arg.Explanation}");
            }
        }

        // ガイドアイコンを登録
        _inputGuideView.RegisterInputGuides();
    }

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
            ( new GuideIcon[]{ Constants.GuideIcon.ALL_CURSOR },         "", null, null, 0.0f, -1),
            ( new GuideIcon[]{ Constants.GuideIcon.VERTICAL_CURSOR },    "", null, null, 0.0f, -1),
            ( new GuideIcon[]{ Constants.GuideIcon.HORIZONTAL_CURSOR },  "", null, null, 0.0f, -1),
            ( new GuideIcon[]{ Constants.GuideIcon.CONFIRM },            "", null, null, 0.0f, -1),
            ( new GuideIcon[]{ Constants.GuideIcon.CANCEL },             "", null, null, 0.0f, -1),
            ( new GuideIcon[]{ Constants.GuideIcon.TOOL},                "", null, null, 0.0f, -1),
            ( new GuideIcon[]{ Constants.GuideIcon.INFO},                "", null, null, 0.0f, -1),
            ( new GuideIcon[]{ Constants.GuideIcon.OPT1},                "", null, null, 0.0f, -1),
            ( new GuideIcon[]{ Constants.GuideIcon.OPT2},                "", null, null, 0.0f, -1),
            ( new GuideIcon[]{ Constants.GuideIcon.SUB1},                "", null, null, 0.0f, -1),
            ( new GuideIcon[]{ Constants.GuideIcon.SUB2},                "", null, null, 0.0f, -1),
            ( new GuideIcon[]{ Constants.GuideIcon.SUB3},                "", null, null, 0.0f, -1),
            ( new GuideIcon[]{ Constants.GuideIcon.SUB4 },               "", null, null, 0.0f, -1),
#if UNITY_EDITOR
            ( new GuideIcon[]{ Constants.GuideIcon.DEBUG_MENU },         "", null, null, 0.0f, -1)
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