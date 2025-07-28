using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FocusRoutineBase : MonoBehaviour, IFocusRoutine
{
    private FocusState _focusState = FocusState.NONE;

    virtual public void Init()
    {
        _focusState = FocusState.EXIT;
    }

    virtual public void UpdateRoutine() { }
    virtual public void LateUpdateRoutine() { }
    virtual public void ScheduleRun()
    {
        _focusState = FocusState.RUN_SCHEDULED;
    }
    virtual public void Run()
    {
        Init();

        _focusState = FocusState.RUN;

        gameObject.SetActive(true);
    }
    virtual public void Restart()
    {
        _focusState = FocusState.RUN;

        gameObject.SetActive(true);
    }
    virtual public void Pause()
    {
        _focusState = FocusState.PAUSE;

        gameObject.SetActive(false);
    }
    virtual public void ScheduleExit()
    {
        _focusState = FocusState.EXIT_SCHEDULED;
    }
    virtual public void Exit()
    {
        _focusState = FocusState.EXIT;

        gameObject.SetActive(false);
    }
    virtual public bool IsMatchFocusState(FocusState state)
    {
        return _focusState == state;
    }
    virtual public int GetPriority() { return (int)FocusRoutinePriority.NONE; }
}