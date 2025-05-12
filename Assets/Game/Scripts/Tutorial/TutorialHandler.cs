using Frontier.Battle;
using Frontier.Stage;
using Frontier;
using UnityEngine;
using System.Collections.ObjectModel;
using Zenject;
using static Constants;
using System;
using System.Collections.Generic;
using static TutorialFacade;

public class TutorialHandler : MonoBehaviour
{
    private HierarchyBuilder _hierarchyBld  = null;
    private InputFacade _inputFcd           = null;
    private TutorialPresenter _tutorialView = null;
    private TutorialFileLoader _tutorialLdr = null;
    private List<MonoBehaviour> _bhvList    = new List<MonoBehaviour>();
    private int _currentPageIndex           = 0;
    private float _timeScale                = 1.0f;    

    // チュートリアルデータの参照
    private ReadOnlyCollection<TutorialData> _tutorialDatas = null;
    // 参照しているチュートリアルデータの内容
    private ReadOnlyCollection<TutorialElement> _displayContents = null;
    // 表示済みのトリガータイプ
    private readonly HashSet<TutorialFacade.TriggerType> _shownTriggers = new();

    [Inject]
    public void Construct(HierarchyBuilder hierarchyBld, InputFacade inputFcd )
    {
        _hierarchyBld   = hierarchyBld;
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
            Debug.LogError("チュートリアルデータの読み込みに失敗しました。");
            return;
        }

        _tutorialLdr.LoadData();
    }

    /// <summary>
    /// チュートリアル表示中に動作を停止させるBehaviourを登録します
    /// </summary>
    /// <param name="bhv">対象のBehaviour</param>
    public void RegisterBehaviour(MonoBehaviour bhv)
    {
        if (bhv == null) return;
        
        _bhvList.Add(bhv);
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
            Debug.LogError($"該当するチュートリアルデータが見つかりませんでした。TriggerType: {trigger}");
            return false;
        }

        // チュートリアルの表示
        ShowTutorial(matchingData);

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
    /// チュートリアルの表示中に動作を停止させるBehaviourのtimeScaleを設定します
    /// </summary>
    /// <param name="time">設定するTimeScale値</param>
    private void SetBehavioursTimeScale( float time )
    {
        foreach( var bvh in _bhvList )
        {
            if (bvh != null)
            {
                bvh.enabled = false;
                bvh.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// チュートリアルを表示します
    /// </summary>
    private void ShowTutorial( TutorialData matchingData )
    {
        // 入力コードの登録
        RegistInputCodes();

        // 該当したチュートリアルのデータを表示する
        _displayContents = matchingData.GetTutorialElements.AsReadOnly();
        _tutorialView.ShowTutorial(_displayContents, _displayContents.Count, matchingData.GetFlagBitIdx);

        // 時間速度の変更
        _timeScale      = Time.timeScale;
        SetBehavioursTimeScale(0);
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
        // 時間速度を元に戻す
        Time.timeScale = _timeScale;
        // 表示済みのトリガータイプをクリア
        TutorialFacade.Clear();

        foreach (var bvh in _bhvList)
        {
            if (bvh != null)
            {
                bvh.enabled = true;
                bvh.gameObject.SetActive(true);
            }
        }
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
        return true;
    }

    /// <summary>
    /// 決定ボタンの入力を受け付けられるかどうかを判定します
    /// </summary>
    /// <returns>受付可否</returns>
    private bool CanAcceptConfirm()
    {
        return true;
    }

    /// <summary>
    /// キャンセルボタンの入力を受け付けられるかどうかを判定します
    /// </summary>
    /// <returns>受付可否</returns>
    private bool CanAcceptCancel()
    {
        return true;
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
}