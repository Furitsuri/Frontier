using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontier;
using Zenject;

public class TutorialFacade : BaseFacadeWithFocusRoutineHandler<TutorialHandler, TutorialPresenter>
{
    public enum TriggerType
    {
        OpenBattleCommand,
        LearnParrySkill,
        StartTutorialBattle,
        // 他にも条件が増えていく
    }

    private IUiSystem _uiSystem                                 = null;
    private ISaveHandler<TutorialSaveData> _saveHdlr            = null;
    private static readonly List<TriggerType> _pendingTriggers  = new();

    // 表示済みのトリガータイプ
    private TutorialSaveData _saveData = null;

    [Inject]
    public void Construct(IUiSystem uiSystem, TutorialHandler tutorialHdlr, ISaveHandler<TutorialSaveData> saveHandler)
    {
        _uiSystem       = uiSystem;
        _saveHdlr       = saveHandler;
    }

    /// <summary>
    /// 初期化します
    /// </summary>
    override public void Init()
    {
        // base.Init()は呼び出さない

        if (presenter == null)
        {
            presenter = _uiSystem.GeneralUi.TutorialView;
            NullCheck.AssertNotNull(presenter);
        }

        _saveData = _saveHdlr.Load();

        handler.Init( presenter );
        presenter.Init();
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
            if( handler.ShowTutorial(trigger) )
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
    static public void Notify(TriggerType type)
    {
        if (!_pendingTriggers.Contains(type))
        {
            _pendingTriggers.Add(type);
        }
    }

    /// <summary>
    /// 通知済みのトリガーをクリアします
    /// </summary>
    static public void Clear()
    {
        _pendingTriggers.Clear();
    }

    /// <summary>
    /// チュートリアルの処理を行うハンドラを取得します
    /// </summary>
    /// <returns>ハンドラ</returns>
    public IFocusRoutine GetFocusRoutine()
    {
        return handler;
    }
}