using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using static Constants;

public class StageEditorHandler : IEditorHandler
{
    private InputFacade _inputFcd = null;
    private StageEditorPresenter _stageEditorView = null;

    private IFocusRoutine _focusRoutine = null;

    [Inject]
    public void Construct(InputFacade inputFcd)
    {
        _inputFcd = inputFcd;
    }

    public void Init( StageEditorPresenter stageEditorView )
    {
        _stageEditorView    = stageEditorView;
    }

    public void Update()
    {
    }

    public void Run()
    {
        RegisterInputCodes();

        _stageEditorView.ToggleView();
    }

    /// <summary>
    /// エディターを終了します
    /// </summary>
    public void Exit()
    {
        // 入力コードの登録を解除
        _inputFcd.UnregisterInputCodes();

        _stageEditorView.ToggleView();
    }

    private void RegisterInputCodes()
    {
        _inputFcd.RegisterInputCodesInDebug(
            (GuideIcon.VERTICAL_CURSOR, "SELECT", CanAcceptDirection,   new AcceptDirectionInput(AcceptDirection), MENU_DIRECTION_INPUT_INTERVAL),
            (GuideIcon.CONFIRM,         "CONFIRM", CanAcceptConfirm,    new AcceptBooleanInput(AcceptConfirm), 0.0f),
            (GuideIcon.CANCEL,          "CANCEL", CanAcceptCancel,      new AcceptBooleanInput(AcceptCancel), 0.0f),
            (GuideIcon.OPT2,            "EXIT", CanAcceptOptional,      new AcceptBooleanInput(AcceptOptional), 0.0f)
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

    private bool CanAcceptOptional()
    {
        return true;
    }

    private bool AcceptDirection(Direction dir)
    {
        return false;
    }

    /// <summary>
    /// 決定入力を受け取った際の処理を行います
    /// </summary>
    /// <param name="isInput">決定入力</param>
    /// <returns>入力実行の有無</returns>
    private bool AcceptConfirm(bool isInput)
    {
        
        return false;
    }

    /// <summary>
    /// キャンセル入力を受け取った際の処理を行います
    /// </summary>
    /// <param name="isInput">キャンセル入力</param>
    /// <returns>入力実行の有無</returns>
    private bool AcceptCancel(bool isInput)
    {

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
            _focusRoutine.ScheduleExit();

            return true;
        }

        return false;
    }

    public void SetFocusRoutine( IFocusRoutine routine )
    {
        _focusRoutine = routine;
    }
}
