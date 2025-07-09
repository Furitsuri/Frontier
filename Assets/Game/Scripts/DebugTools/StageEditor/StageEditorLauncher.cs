using Frontier;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class StageEditorLauncher : IDebugLauncher
{
    private DiContainer _diContainer        = null;
    private HierarchyBuilderBase _hierarchyBld  = null;
    private IUiSystem _uiSystem              = null;

    private StageEditorHandler _stageEditorHdlr     = null;
    private StageEditorPresenter _stageEditorView   = null;
    private DebugEditorMonoDriver _debugEditorMonoDrv = null;

    [Inject]
    public void Construct( DiContainer diContainer, HierarchyBuilderBase hierarchyBld, IUiSystem uiSystem, DebugEditorMonoDriver debugEditorMonoDrv )
    {
        _diContainer    = diContainer;
        _hierarchyBld   = hierarchyBld;
        _uiSystem       = uiSystem;
        _debugEditorMonoDrv = debugEditorMonoDrv;
    }

    /// <summary>
    /// 初期化します。
    /// </summary>
    public void Init()
    {   
        _stageEditorHdlr = _hierarchyBld.InstantiateWithDiContainer<StageEditorHandler>(false);
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
        _debugEditorMonoDrv.Init(_stageEditorHdlr);
        _debugEditorMonoDrv.ScheduleRun();
    }
}