using Frontier;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using static Constants;

public enum DebugMainMenu
{
    None = 0,       // デバッグメニューなし
    StageEditor,    // ステージエディター
    DebugUI,        // デバッグUI
    InputGuide,     // 入力ガイド
    Tutorial,       // チュートリアル
}

public class DebugModeController
{
    private GameObject _debugMenuUI;

    private InputFacade _inputFcd = null;

    [Inject]
    public void Construct(InputFacade inputFcd)
    {
        _inputFcd = inputFcd;
    }

    /// <summary>
    /// 初期化します
    /// </summary>
    public void Init()
    {
        _debugMenuUI = GameObject.Find("DebugUI");
    }

    /// <summary>
    /// デバッグメニューを開きます
    /// </summary>
    public void OpenMenu()
    {
        if (_debugMenuUI == null)
        {
            Debug.LogError("Debug Menu UI is not found.");
            return;
        }

        // デバッグメニューを開く
        ToggleDebugMenu();
        // 入力コードを登録
        RegisterInputCode(); 
    }

    private void ToggleDebugMenu()
    {
        _debugMenuUI.SetActive(!_debugMenuUI.activeSelf);
        Time.timeScale = _debugMenuUI.activeSelf ? 0f : 1f; // 一時停止
    }

    /// <summary>
    /// 入力コードを登録します
    /// </summary>
    private void RegisterInputCode()
    {
        _inputFcd.RegisterInputCodes(
            (GuideIcon.VERTICAL_CURSOR, "SELECT",   CanAcceptDirection, new AcceptDirectionInput(AcceptDirection), 0.0f),
            (GuideIcon.CONFIRM,         "CONFIRM",  CanAcceptConfirm,   new AcceptBooleanInput(AcceptConfirm), 0.0f),
            (GuideIcon.CANCEL,          "TURN END", CanAcceptCancel,    new AcceptBooleanInput(AcceptCancel), 0.0f)
        );
    }

    private bool CanAcceptDirection()
    {
        return true;
    }

    private bool CanAcceptConfirm()
    {
        return true;
    }

    private bool CanAcceptCancel()
    {
        return true;
    }

    private bool AcceptDirection(Direction dir)
    {
        return true;
    }

    private bool AcceptConfirm(bool isInput)
    {
        return true;
    }

    private bool AcceptCancel(bool isInput)
    {
        return true;
    }
}