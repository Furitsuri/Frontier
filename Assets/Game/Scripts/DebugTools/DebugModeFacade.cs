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
    Tutorial,       // チュートリアル

    MAX,
}

public class DebugModeFacade
{
    private HierarchyBuilder _hierarchyBld      = null;
    private UISystem _uiSystem                  = null;
    private DebugMenuHandler _debugMenuHdlr     = null;
    private DebugMenuPresenter _debugMenuView   = null;
    private GameObject _debugUi                 = null;

    [Inject]
    public void Construct( HierarchyBuilder hierarchyBld, UISystem uiSystem )
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

        if (_debugMenuHdlr == null)
        {
            _debugMenuHdlr = _hierarchyBld.InstantiateWithDiContainer<DebugMenuHandler>();
            NullCheck.AssertNotNull(_debugMenuHdlr);
        }

        if( _debugMenuView == null )
        {
            _debugMenuView = _uiSystem.DebugUi.DebugMenuView;
            NullCheck.AssertNotNull(_debugMenuView);
        }

        _debugMenuView.Init();
        _debugMenuHdlr.Init(_debugMenuView, ToggleDebugCallback);
    }

    /// <summary>
    /// 更新を行います
    /// </summary>
    public void Update()
    {
        if (!_debugUi.activeSelf) return;

        // デバッグメニューの更新
        _debugMenuHdlr.Update();
    }

    /// <summary>
    /// デバッグメニューを開きます
    /// </summary>
    public void OpenDebugMenu()
    {
        // デバッグメニューを開く
        _debugMenuHdlr.OpenMenu();
    }

    /// <summary>
    /// デバッグモードを切り替える際のコールバック関数です。
    /// </summary>
    public void ToggleDebugCallback()
    {
        _debugUi.SetActive(!_debugUi.activeSelf);
    }
}