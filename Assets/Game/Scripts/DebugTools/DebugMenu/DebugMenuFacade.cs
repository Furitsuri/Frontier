using Frontier;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Zenject;
using static Constants;

public enum DebugMainMenu
{
    NONE = -1,      // デバッグメニューなし

    BATTLE,         // 戦闘
    TUTORIAL,       // チュートリアル

    MAX,
}

public class DebugMenuFacade : BaseFacadeWithFocusRoutineHandler<DebugMenuHandler, DebugMenuPresenter>
{
    private GameObject _debugUi                     = null;

    /// <summary>
    /// 初期化します
    /// </summary>
    public void Init( InputCode.EnableCallback canAcceptCb, AcceptBooleanInput.AcceptBooleanInputCallback acceptInputCb )
    {
        _debugUi = GameObjectFinder.FindInSceneEvenIfInactive("DebugUI");
        if (null == _debugUi)
        {
            LogHelper.LogError("Debug Menu UI is not found in the scene.");
            return;
        }

        _debugUi.SetActive(false); // 初期状態では非表示

        presenter.Init();
        handler.Init(presenter, ToggleDebugCallback, canAcceptCb, acceptInputCb);
    }

    /// <summary>
    /// デバッグメニューを開きます
    /// </summary>
    public void OpenDebugMenu()
    {
        handler.ScheduleRun();
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
        return handler;
    }
}