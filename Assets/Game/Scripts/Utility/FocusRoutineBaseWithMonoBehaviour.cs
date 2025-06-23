using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FocusRoutineBaseWithMonoBehaviour : MonoBehaviour, IFocusRoutine
{
    private FocusState _focusState = FocusState.NONE;

    virtual public void Init()
    {
        _focusState = FocusState.EXIT;
    }

    virtual public void UpdateRoutine() { }
    virtual public void ScheduleRun()
    {
        _focusState = FocusState.RUN_SCHEDULED;
    }
    virtual public void Run()
    {
        _focusState = FocusState.RUN;
    }
    virtual public void Restart()
    {
        _focusState = FocusState.RUN;
    }
    virtual public void Pause()
    {
        _focusState = FocusState.PAUSE;
    }
    virtual public void ScheduleExit()
    {
        _focusState = FocusState.EXIT_SCHEDULED;
    }
    virtual public void Exit()
    {
        _focusState = FocusState.EXIT;
    }
    virtual public bool IsMatchFocusState(FocusState state)
    {
        return _focusState == state;
    }
    virtual public int GetPriority() { return (int)FocusRoutinePriority.NONE; }
}