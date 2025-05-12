using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontier;
using Zenject;

public class TutorialFacade
{
    public enum TriggerType
    {
        OpenBattleCommand,
        LearnParrySkill,
        StartTutorialBattle,
        // 他にも条件が増えていく
    }

    private HierarchyBuilder _hierarchyBld      = null;
    private UISystem _uiSystem                  = null;
    private TutorialPresenter _tutorialView     = null;
    private TutorialHandler _tutorialHdl        = null;
    private static readonly List<TriggerType> _pendingTriggers = new();

    // 表示済みのトリガータイプ
    private readonly HashSet<TutorialFacade.TriggerType> _shownTriggers = new();

    [Inject]
    public void Construct(HierarchyBuilder hierarchyBld, UISystem uiSystem)
    {
        _hierarchyBld   = hierarchyBld;
        _uiSystem       = uiSystem;
    }

    /// <summary>
    /// 初期化します
    /// </summary>
    public void Init()
    {
        if (_tutorialHdl == null)
        {
            _tutorialHdl = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<TutorialHandler>(true, false);
            NullCheck.AssertNotNull(_tutorialHdl);
        }

        if (_tutorialView == null)
        {
            _tutorialView = _uiSystem.GeneralUi.TutorialView;
            NullCheck.AssertNotNull(_tutorialView);
        }

        _tutorialHdl.Init( _tutorialView );
        _tutorialView.Init();
    }

    /// <summary>
    /// チュートリアルの表示を試行します
    /// </summary>
    public void TryShowTutorial()
    {
        foreach (var trigger in _pendingTriggers)
        {
            if (_shownTriggers.Contains(trigger)) continue;

            // チュートリアルを表示します
            if( _tutorialHdl.ShowTutorial(trigger) )
            {
                // 表示済みのトリガータイプに追加
                _shownTriggers.Add(trigger);
            }
        }
    }

    /// <summary>
    /// チュートリアル表示中に動作を停止させるBehvaiourを登録します
    /// </summary>
    /// <param name="target">動作を停止させる対象</param>
    public void RegisterPauseTarget(IGamePauseTarget target)
    {
        var bhv = target.GetUnderlyingBehaviour();
        _tutorialHdl.RegisterBehaviour(bhv);
    }

    /// <summary>
    /// チュートリアルのトリガーを通知します
    /// 通知されたトリガーは、チュートリアル表示処理の際に使用されます
    /// </summary>
    /// <param name="type">通知するトリガータイプ</param>
    public static void Notify(TriggerType type)
    {
        if (!_pendingTriggers.Contains(type))
        {
            _pendingTriggers.Add(type);
        }
    }

    /// <summary>
    /// 通知済みのトリガーをクリアします
    /// </summary>
    public static void Clear()
    {
        _pendingTriggers.Clear();
    }
}