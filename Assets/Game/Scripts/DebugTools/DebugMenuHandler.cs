using Frontier;
using System;
using System.Collections.ObjectModel;
using TMPro;
using UnityEngine;
using Zenject;
using static Constants;

public class DebugMenuHandler
{
    private InputFacade _inputFcd = null;

    private DebugMenuPresenter _debugMenuView = null;
    private ReadOnlyCollection<TextMeshProUGUI> _menuTexts;
    // 選択中のメニューインデックス
    private int _selectedMenuIndex = 0;

    public delegate void ToggleDebugCallback();
    public ToggleDebugCallback _toggleDebugCb = null;

    [Inject]
    public void Construct( InputFacade inputFcd )
    {
        _inputFcd       = inputFcd;
    }

    /// <summary>
    /// 初期化します
    /// </summary>
    public void Init( DebugMenuPresenter debugMenuView, ToggleDebugCallback cb )
    {
        _debugMenuView      = debugMenuView;
        _menuTexts          = _debugMenuView.MenuTexts;
        _selectedMenuIndex  = 0;
        _toggleDebugCb      = cb;
    }

    /// <summary>
    /// 更新を行います
    /// </summary>
    public void Update()
    {
        _debugMenuView.UpdateMenuCursor( _selectedMenuIndex );
    }

    /// <summary>
    /// デバッグメニューを開きます
    /// </summary>
    public void OpenMenu()
    {
        // 現在の入力コードを一時退避した上で新たに登録
        BackupAndRegisterInputCode();

        // デバッグメニューを開く
        ToggleDebugMenu();
    }

    /// <summary>
    /// デバッグメニューを閉じます
    /// </summary>
    public void CloseMenu()
    {
        // デバッグメニューを閉じる
        ToggleDebugMenu();

        // 以前の入力コードの登録内容を復旧
        _inputFcd.RestoreInputCodes(); ;
    }

    /// <summary>
    /// デバッグメニューの表示/非表示を切り替えます
    /// </summary>
    private void ToggleDebugMenu()
    {
        _toggleDebugCb?.Invoke(); // コールバックを呼び出す
    }

    /// <summary>
    /// 入力コードを登録します
    /// </summary>
    private void BackupAndRegisterInputCode()
    {
        _inputFcd.BackupInputCodes();
        _inputFcd.UnregisterInputCodes(true);

        _inputFcd.RegisterInputCodes(
            (GuideIcon.VERTICAL_CURSOR, "SELECT",   CanAcceptDirection, new AcceptDirectionInput(AcceptDirection),  0.23f),
            (GuideIcon.CONFIRM,         "CONFIRM",  CanAcceptConfirm,   new AcceptBooleanInput(AcceptConfirm),      0.0f),
            (GuideIcon.ESCAPE,          "EXIT",     CanAcceptOptional,  new AcceptBooleanInput(AcceptOptional),     0.0f)
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

    private bool AcceptConfirm(bool isInput)
    {
        return true;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="isInput"></param>
    /// <returns></returns>
    private bool AcceptOptional(bool isInput)
    {
        if( isInput ) 
        {
            // メニューを閉じる
            CloseMenu();
            return true;
        }

        return false;
    }
}
