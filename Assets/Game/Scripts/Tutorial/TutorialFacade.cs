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

    private HierarchyBuilderBase _hierarchyBld              = null;
    private IUiSystem _uiSystem                          = null;
    private TutorialPresenter _tutorialView             = null;
    private TutorialHandler _tutorialHdl                = null;
    private ISaveHandler<TutorialSaveData> _saveHdlr    = null;
    private static readonly List<TriggerType> _pendingTriggers = new();

    // 表示済みのトリガータイプ
    private TutorialSaveData _saveData = null;

    [Inject]
    public void Construct(HierarchyBuilderBase hierarchyBld, IUiSystem uiSystem, ISaveHandler<TutorialSaveData> saveHandler)
    {
        _hierarchyBld   = hierarchyBld;
        _uiSystem       = uiSystem;
        _saveHdlr       = saveHandler;
    }

    /// <summary>
    /// 初期化します
    /// </summary>
    public void Init()
    {
        if (_tutorialHdl == null)
        {
            _tutorialHdl = _hierarchyBld.InstantiateWithDiContainer<TutorialHandler>(false);
            NullCheck.AssertNotNull(_tutorialHdl);
        }

        if (_tutorialView == null)
        {
            _tutorialView = _uiSystem.GeneralUi.TutorialView;
            NullCheck.AssertNotNull(_tutorialView);
        }

        _saveData = _saveHdlr.Load();

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
            if (_saveData._shownTriggers.Contains(trigger)) continue;

            // チュートリアルを表示
            if( _tutorialHdl.ShowTutorial(trigger) )
            {
                // 表示済みのトリガータイプに追加、保存
                _saveData._shownTriggers.Add(trigger);
                _saveHdlr.Save(_saveData);
            }
        }
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

    /// <summary>
    /// チュートリアルの処理を行うハンドラを取得します
    /// </summary>
    /// <returns>ハンドラ</returns>
    public IFocusRoutine GetFocusRoutine()
    {
        return _tutorialHdl;
    }
}