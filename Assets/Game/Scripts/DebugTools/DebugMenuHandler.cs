using Frontier;
using System;
using System.Collections.ObjectModel;
using TMPro;
using UnityEngine;
using Zenject;
using static Constants;

public class DebugMenuHandler : FocusRoutineBase
{
    private HierarchyBuilderBase _hierarchyBld  = null;
    private InputFacade _inputFcd           = null;

    private DebugMenuPresenter _debugMenuView   = null;
    private IDebugLauncher[] _debugLhr          = null;
    // 選択中のメニューインデックス
    private int _selectedMenuIndex              = 0;
    private ReadOnlyCollection<TextMeshProUGUI> _menuTexts;
    // デバッグON/OFFのコールバック
    public delegate void ToggleDebugCallback();
    public ToggleDebugCallback _toggleDebugCb = null;

    [Inject]
    public void Construct( HierarchyBuilderBase hierarchyBld, InputFacade inputFcd )
    {
        _hierarchyBld   = hierarchyBld;
        _inputFcd       = inputFcd;
    }

    /// <summary>
    /// 初期化します
    /// </summary>
    public void Init( DebugMenuPresenter debugMenuView, ToggleDebugCallback cb )
    {
        base.Init();

        _debugMenuView      = debugMenuView;
        _menuTexts          = _debugMenuView.MenuTexts;
        _selectedMenuIndex  = 0;
        _toggleDebugCb      = cb;
        _debugLhr           = new IDebugLauncher[(int)DebugMainMenu.MAX];
    }

    /// <summary>
    /// デバッグ画面全体とデバッグメニューの表示・非表示を切り替えます
    /// </summary>
    private void ToggleDebugView()
    {
        _toggleDebugCb?.Invoke();
        _debugMenuView.ToggleMenuVisibility();
    }

    private void RegisterInputCodes()
    {
        _inputFcd.RegisterInputCodesInDebug(
            (GuideIcon.VERTICAL_CURSOR, "SELECT",   CanAcceptDirection, new AcceptDirectionInput(AcceptDirection),  MENU_DIRECTION_INPUT_INTERVAL),
            (GuideIcon.CONFIRM,         "CONFIRM",  CanAcceptConfirm,   new AcceptBooleanInput(AcceptConfirm),      0.0f),
            (GuideIcon.ESCAPE,          "EXIT",     CanAcceptOptional,  new AcceptBooleanInput(AcceptOptional),     0.0f)
        );
    }

    /// <summary>
    /// 指定のIndexに対応するデバッグメニューを起動します
    /// </summary>
    /// <param name="menuIdx">指定するIndex値</param>
    private void LaunchDebugMenu( int menuIdx )
    {
        if (_debugLhr[menuIdx] == null)
        {
            switch( menuIdx )
            {
                case (int)DebugMainMenu.STAGE_EDITOR:
                    // ステージエディターのインスタンスを生成
                    _debugLhr[menuIdx] = _hierarchyBld.InstantiateWithDiContainer<StageEditorLauncher>(false);
                    break;
                case (int)DebugMainMenu.BATTLE:
                    break;
                case (int)DebugMainMenu.TUTORIAL:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(menuIdx), "Invalid menu index for debug launcher.");
            }

            NullCheck.AssertNotNull(_debugLhr[menuIdx]);
        }

        // 現在の入力コードを抹消
        _inputFcd.UnregisterInputCodes();
        
        _debugLhr[menuIdx].Init();
        _debugLhr[menuIdx].LaunchEditor();
    }

    private bool CanAcceptDirection()
    {
        return true;
    }

    private bool CanAcceptConfirm()
    {
        return true;
    }

    private bool CanAcceptOptional()
    {
        return true;
    }

    private bool AcceptDirection(Direction dir)
    {
        if(dir == Direction.FORWARD)
        {
            // 前のメニューへ
            _selectedMenuIndex = (_selectedMenuIndex - 1 + _menuTexts.Count) % _menuTexts.Count;

            return true;
        }
        else if(dir == Direction.BACK)
        {
            // 次のメニューへ
            _selectedMenuIndex = (_selectedMenuIndex + 1) % _menuTexts.Count;

            return true;
        }

        return false;
    }

    /// <summary>
    /// 決定入力を受け取った際の処理を行います
    /// </summary>
    /// <param name="isInput">決定入力</param>
    /// <returns>入力実行の有無</returns>
    private bool AcceptConfirm(bool isInput)
    {
        if( isInput )
        {
            Pause();
            LaunchDebugMenu(_selectedMenuIndex);

            return true;
        }

        return false;
    }

    /// <summary>
    /// オプション入力を受けた際の処理を行います
    /// </summary>
    /// <param name="isInput">オプション入力</param>
    /// <returns>入力実行の有無</returns>
    private bool AcceptOptional(bool isInput)
    {
        if( isInput ) 
        {
            ScheduleExit();

            return true;
        }

        return false;
    }

    // =========================================================
    // IFocusRoutine 実装
    // =========================================================

    #region IFocusRoutine Implementation

    /// <summary>
    /// 更新を行います
    /// </summary>
    override public void UpdateRoutine()
    {
        _debugMenuView.UpdateMenuCursor(_selectedMenuIndex);
    }

    override public void Run()
    {
        base.Run();

        ToggleDebugView();
        _inputFcd.UnregisterInputCodes();
        RegisterInputCodes();
    }

    override public void Restart()
    {
        base.Restart();

        _debugMenuView.ToggleMenuVisibility();
        _inputFcd.UnregisterInputCodes();
        RegisterInputCodes();
    }

    override public void Pause()
    {
        base.Pause();

        _debugMenuView.ToggleMenuVisibility();
        _inputFcd.UnregisterInputCodes();
    }

    override public void Exit()
    {
        base.Exit();

        ToggleDebugView();
        _inputFcd.UnregisterInputCodes();
    }

    override public int GetPriority() { return (int)FocusRoutinePriority.DEBUG_MENU; }

    #endregion  // IFocusRoutine 実装
}
