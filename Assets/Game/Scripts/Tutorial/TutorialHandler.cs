using Frontier.Battle;
using Frontier.Stage;
using Frontier;
using UnityEngine;
using System.Collections.ObjectModel;
using Zenject;
using static Constants;
using System;
using static TutorialFacade;
using Unity.VisualScripting;

public class TutorialHandler : BaseHandlerExtendedFocusRoutine
{
    private InputFacade _inputFcd           = null;
    private TutorialPresenter _tutorialView = null;
    private TutorialFileLoader _tutorialLdr = null;
    private int _currentPageIndex           = 0;

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
        base.Init();

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
        ScheduleRun();

        return true;
    }

    /// <summary>
    /// チュートリアル画面における入力コードを登録します
    /// </summary>
    private void RegistInputCodes()
    {
        int hashCode = Hash.GetStableHash(GetType().Name);

        _inputFcd.RegisterInputCodes(
            (GuideIcon.HORIZONTAL_CURSOR,   "PAGE TRANSACTION", CanAcceptDirection, new AcceptDirectionInput(AcceptDirection),  MENU_DIRECTION_INPUT_INTERVAL, hashCode),
            (GuideIcon.CONFIRM,             "NEXT",             CanAcceptConfirm, new AcceptBooleanInput(AcceptConfirm), 0.0f, hashCode),
            (GuideIcon.CANCEL,              "BACK",             CanAcceptCancel, new AcceptBooleanInput(AcceptCancel), 0.0f, hashCode)
         );
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
        return IsMatchFocusState( FocusState.RUN );
    }

    /// <summary>
    /// 決定ボタンの入力を受け付けられるかどうかを判定します
    /// </summary>
    /// <returns>受付可否</returns>
    private bool CanAcceptConfirm()
    {
        return IsMatchFocusState(FocusState.RUN);
    }

    /// <summary>
    /// キャンセルボタンの入力を受け付けられるかどうかを判定します
    /// </summary>
    /// <returns>受付可否</returns>
    private bool CanAcceptCancel()
    {
        return IsMatchFocusState(FocusState.RUN);
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
            // チュートリアルの終了を予約
            ScheduleExit();
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

        // チュートリアルの終了を予約
        ScheduleExit();

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

    // =========================================================
    // IFocusRoutine 実装
    // =========================================================
    #region IFocusRoutine Implementation

    override public void Run()
    {
        base.Run();

        // 入力コードを登録
        RegistInputCodes();
    }

    override public void Restart()
    {
        base.Restart();

        // 入力コードを再登録
        RegistInputCodes();
    }

    /// <summary>
    /// IFocusRoutineの実装です
    /// 処理を中断します
    /// </summary>
    override public void Pause()
    {
        base.Pause();

        // 入力コードの解除
        _inputFcd.UnregisterInputCodes(Hash.GetStableHash(GetType().Name));
    }

    /// <summary>
    /// IFocusRoutineの実装です
    /// 処理を停止します
    /// </summary>
    override public void Exit()
    {
        base.Exit();

        // チュートリアルの終了
        _tutorialView.Exit();
        // 入力コードの解除
        _inputFcd.UnregisterInputCodes(Hash.GetStableHash(GetType().Name));
        // 表示済みのトリガータイプをクリア
        TutorialFacade.Clear();
    }

    override public int GetPriority() { return (int)FocusRoutinePriority.TUTORIAL; }

    #endregion
}