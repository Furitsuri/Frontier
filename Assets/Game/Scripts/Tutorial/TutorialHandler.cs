using Frontier.Battle;
using Frontier.Stage;
using Frontier;
using UnityEngine;
using System.Collections.ObjectModel;
using Zenject;
using static Constants;
using System;
using static TutorialFacade;

public class TutorialHandler : IFocusRoutine
{
    private InputFacade _inputFcd           = null;
    private TutorialPresenter _tutorialView = null;
    private TutorialFileLoader _tutorialLdr = null;
    private int _currentPageIndex           = 0;
    private FocusState _focusState          = FocusState.NONE;

    // チュートリアルデータの参照
    private ReadOnlyCollection<TutorialData> _tutorialDatas = null;
    // 参照しているチュートリアルデータの内容
    private ReadOnlyCollection<TutorialElement> _displayContents = null;

    [Inject]
    public void Construct( InputFacade inputFcd )
    {
        _inputFcd       = inputFcd;
    }

    /// <summary>
    /// 初期化します
    /// </summary>
    /// <param name="turtorialView">チュートリアル表示クラス</param>
    public void Init(TutorialPresenter turtorialView)
    {
        _tutorialView = turtorialView;

        GameObject obj = GameObject.Find("Tutorial");
        if( obj != null )
        {
            _tutorialLdr = obj.GetComponent<TutorialFileLoader>();
        }

        if (_tutorialLdr == null)
        {
            LogHelper.LogError("チュートリアルデータの読み込みに失敗しました。");
            return;
        }

        _tutorialLdr.LoadData();

        _focusState = FocusState.PAUSE;
    }

    /// <summary>
    /// 指定されたチュートリアルの表示を行います
    /// </summary>
    /// <param name="trigger">指定するチュートリアルの種別</param>
    /// <returns>表示の成否</returns>
    public bool ShowTutorial( TriggerType trigger)
    {
        _tutorialDatas = Array.AsReadOnly(_tutorialLdr.GetTutorialDatas());

        var matchingData = FindMatchingTutorialData(trigger);
        if( null == matchingData )
        {
            LogHelper.LogError($"該当するチュートリアルデータが見つかりませんでした。TriggerType: {trigger}");
            return false;
        }

        // 該当したチュートリアルのデータを表示する
        _displayContents = matchingData.GetTutorialElements.AsReadOnly();
        _tutorialView.ShowTutorial(_displayContents, _displayContents.Count, matchingData.GetFlagBitIdx);

        // 実行を予約
        _focusState = FocusState.RESERVE;

        return true;
    }

    /// <summary>
    /// チュートリアル画面における入力コードを登録します
    /// </summary>
    private void RegistInputCodes()
    {
        _inputFcd.RegisterInputCodes(
            (GuideIcon.HORIZONTAL_CURSOR,   "PAGE TRANSACTION", CanAcceptDirection, new AcceptDirectionInput(AcceptDirection),  DIRECTION_INPUT_INTERVAL),
            (GuideIcon.CONFIRM,             "NEXT",             CanAcceptConfirm,   new AcceptBooleanInput(AcceptConfirm),      0.0f),
            (GuideIcon.CANCEL,              "BACK",             CanAcceptCancel,    new AcceptBooleanInput(AcceptCancel),       0.0f)
         );
    }

    /// <summary>
    /// チュートリアルを終了します
    /// </summary>
    private void ExitTutorial()
    {
        // チュートリアルの終了
        _tutorialView.Exit();
        // 入力コードの解除
        _inputFcd.ResetInputCodes();
        // 表示済みのトリガータイプをクリア
        TutorialFacade.Clear();

        _focusState = FocusState.EXIT;
    }

    /// <summary>
    /// 前のチュートリアルページに遷移します
    /// </summary>
    private void TransitPrevPage()
    {
        _currentPageIndex--;
        if(_currentPageIndex < 0)
        {
            _currentPageIndex = 0;
            return;
        }

        _tutorialView.SwitchPage(_currentPageIndex);
    }

    /// <summary>
    /// 次のチュートリアルページに遷移します
    /// </summary>
    private void TransitNextPage()
    {
        _currentPageIndex++;
        if (_currentPageIndex >= _displayContents.Count)
        {
            _currentPageIndex = _displayContents.Count - 1;
            return;
        }

        _tutorialView.SwitchPage(_currentPageIndex);
    }

    /// <summary>
    /// 方向に対する入力を受け付けられるかどうかを判定します
    /// </summary>
    /// <returns>受付可否</returns>
    private bool CanAcceptDirection()
    {
        return _focusState == FocusState.RUN;
    }

    /// <summary>
    /// 決定ボタンの入力を受け付けられるかどうかを判定します
    /// </summary>
    /// <returns>受付可否</returns>
    private bool CanAcceptConfirm()
    {
        return _focusState == FocusState.RUN;
    }

    /// <summary>
    /// キャンセルボタンの入力を受け付けられるかどうかを判定します
    /// </summary>
    /// <returns>受付可否</returns>
    private bool CanAcceptCancel()
    {
        return _focusState == FocusState.RUN;
    }

    /// <summary>
    /// 方向入力を受けつけた際の処理を行います
    /// </summary>
    /// <param name="dir">方向入力</param>
    /// <returns>入力実行の有無</returns>
    private bool AcceptDirection( Constants.Direction dir )
    {
        switch( dir )
        {
            case Direction.LEFT:
                TransitPrevPage();

                return true;

            case Direction.RIGHT:
                TransitNextPage();

                return true;
        }

        return false;
    }

    /// <summary>
    /// 決定入力を受けつけた際の処理を行います
    /// </summary>
    /// <param name="isInput">入力の有無</param>
    /// <returns>入力実行の有無</returns>
    private bool AcceptConfirm(bool isInput)
    {
        if (!isInput) return false;

        if (_currentPageIndex >= _displayContents.Count - 1)
        {
            // チュートリアルの終了
            ExitTutorial();
        }
        else
        {
            // 次のページへ遷移
            TransitNextPage();
        }

        return true;
    }

    /// <summary>
    /// キャンセル入力を受けつけた際の処理を行います
    /// </summary>
    /// <param name="isInput">入力の有無</param>
    /// <returns>入力実行の有無</returns>
    private bool AcceptCancel(bool isInput)
    {
        if (!isInput) return false;

        // チュートリアルの終了
        ExitTutorial();

        return true;
    }

    /// <summary>
    /// 指定されたトリガータイプに対し、該当するチュートリアルデータを取得します
    /// </summary>
    /// <param name="triggerType">指定するトリガータイプ</param>
    /// <returns>該当したチュートリアルデータ</returns>
    private TutorialData FindMatchingTutorialData( TutorialFacade.TriggerType triggerType )
    {
        foreach (var data in _tutorialDatas)
        {
            if (data.TriggerType == triggerType)
            {
                return data;
            }
        }

        return null;
    }

    public void Run()
    {
        _focusState = FocusState.RUN;

        // 入力コードを登録
        RegistInputCodes();
    }

    public void Restart()
    {
        _focusState = FocusState.RUN;

        // 入力コードを再登録
        RegistInputCodes();
    }

    /// <summary>
    /// IFocusRoutineの実装です
    /// 処理を中断します
    /// </summary>
    public void Pause()
    {
        _focusState = FocusState.PAUSE;

        // 入力コードの解除
        _inputFcd.ResetInputCodes();
    }

    /// <summary>
    /// IFocusRoutineの実装です
    /// 処理を停止します
    /// </summary>
    public void Exit()
    {
        _focusState = FocusState.EXIT;

        // チュートリアルの終了
        _tutorialView.Exit();
        // 入力コードの解除
        _inputFcd.ResetInputCodes();
    }

    /// <summary>
    /// IFocusRoutineの実装です
    /// 指定のFocusStateと一致するか否かを判定します
    /// </summary>
    /// <returns>一致の成否</returns>
    public bool IsMatchFocusState(FocusState state)
    {
        return _focusState == state;
    }

    public int GetPriority() { return (int)FocusRoutinePriority.TUTORIAL; }
}