using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class DebugEditorMonoDriver : FocusRoutineBaseWithMonoBehaviour
{
    private IEditorHandler _editorHnd;

    public void Init(IEditorHandler handler)
    {
        base.Init();

        handler.SetFocusRoutine(this);
        _editorHnd = handler;
    }

    #region IFocusRoutine Implementation

    override public void UpdateRoutine()
    {
        _editorHnd?.Update();
    }

    override public void ScheduleRun()
    {
        base.ScheduleRun();
    }

    override public void Run()
    {
        _editorHnd?.Run();

        base.Run();
    }

    override public void Restart()
    {
        base.Restart();
    }

    override public void Pause()
    {
        base.Pause();
    }

    override public void ScheduleExit()
    {
        base.ScheduleExit();
    }

    override public void Exit()
    {
        _editorHnd?.Exit();

        base.Exit();
    }

    #endregion // IFocusRoutine Implementation
}