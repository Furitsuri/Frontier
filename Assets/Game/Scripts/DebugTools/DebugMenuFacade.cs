using Frontier;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Zenject;
using static Constants;

public enum DebugMainMenu
{
    NONE = -1,      // デバッグメニューなし

    STAGE_EDITOR,   // ステージエディター
    BATTLE,         // 戦闘
    TUTORIAL,       // チュートリアル

    MAX,
}

public class DebugMenuFacade
{
    private HierarchyBuilderBase _hierarchyBld          = null;
    private IUiSystem _uiSystem                     = null;
    private DebugMenuHandler _debugMenuHnd          = null;
    private DebugMenuPresenter _debugMenuView       = null;
    private GameObject _debugUi                     = null;

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
        _debugUi = GameObjectFinder.FindInSceneEvenIfInactive("DebugUI");
        if (null == _debugUi)
        {
            LogHelper.LogError("Debug Menu UI is not found in the scene.");
            return;
        }

        _debugUi.SetActive(false); // 初期状態では非表示

        if (_debugMenuHnd == null)
        {
            _debugMenuHnd = _hierarchyBld.InstantiateWithDiContainer<DebugMenuHandler>(false);
            NullCheck.AssertNotNull(_debugMenuHnd);
        }

        if( _debugMenuView == null )
        {
            _debugMenuView = _uiSystem.DebugUi.DebugMenuView;
            NullCheck.AssertNotNull(_debugMenuView);
        }

        _debugMenuView.Init();
        _debugMenuHnd.Init(_debugMenuView, ToggleDebugCallback);
    }

    /// <summary>
    /// デバッグメニューを開きます
    /// </summary>
    public void OpenDebugMenu()
    {
        _debugMenuHnd.ScheduleRun();
    }

    /// <summary>
    /// デバッグモードを切り替える際のコールバック関数です。
    /// </summary>
    public void ToggleDebugCallback()
    {
        _debugUi.SetActive(!_debugUi.activeSelf);
    }

    /// <summary>
    /// デバッグメニュー処理を行うハンドラを取得します
    /// </summary>
    /// <returns>ハンドラ</returns>
    public IFocusRoutine GetFocusRoutine()
    {
        return _debugMenuHnd;
    }
}