using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class DebugEditorMonoDriver : FocusRoutineBase
{
    private IEditorHandler _editorHnd;

    public void Init(IEditorHandler handler)
    {
        base.Init();

        handler.SetFocusRoutine(this);
        _editorHnd = handler;
    }

    #region IFocusRoutine Implementation

    public override void UpdateRoutine()
    {
        _editorHnd?.Update();
    }

    public override void LateUpdateRoutine()
    {
        _editorHnd?.LateUpdate();
    }

    public override void ScheduleRun()
    {
        base.ScheduleRun();
    }

    public override void Run()
    {
        base.Run();

        _editorHnd?.Run();
    }

    public override void Restart()
    {
        base.Restart();
    }

    public override void Pause()
    {
        base.Pause();
    }

    public override void ScheduleExit()
    {
        base.ScheduleExit();
    }

    public override void Exit()
    {
        _editorHnd?.Exit();

        base.Exit();
    }

    #endregion // IFocusRoutine Implementation
}