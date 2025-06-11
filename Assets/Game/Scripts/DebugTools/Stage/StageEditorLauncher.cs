using Frontier;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class StageEditorLauncher : IDebugLauncher
{
    private DiContainer _diContainer        = null;
    private HierarchyBuilder _hierarchyBld  = null;
    private UISystem _uiSystem              = null;

    private StageEditorHandler _stageEditorHdlr     = null;
    private StageEditorPresenter _stageEditorView   = null;

    [Inject]
    public void Construct( DiContainer diContainer, HierarchyBuilder hierarchyBld, UISystem uiSystem )
    {
        _diContainer    = diContainer;
        _hierarchyBld   = hierarchyBld;
        _uiSystem       = uiSystem;
    }

    /// <summary>
    /// 初期化します。
    /// </summary>
    public void Init()
    {   
        _stageEditorHdlr = _hierarchyBld.InstantiateWithDiContainer<StageEditorHandler>();
        _stageEditorView = _uiSystem.DebugUi.StageEditorView;
        if( _stageEditorHdlr == null || _stageEditorView == null )
        {
            LogHelper.LogError("Stage Editor Handler or View is not initialized properly.");
            return;
        }

        _stageEditorHdlr.Init(_stageEditorView);
        _stageEditorView.Init();
    }

    /// <summary>
    /// Editorを起動します。
    /// </summary>
    public void LaunchEditor()
    {
        var driver = _diContainer.Resolve<DebugEditorMonoDriver>();
        driver.Init(_stageEditorHdlr);
    }
}