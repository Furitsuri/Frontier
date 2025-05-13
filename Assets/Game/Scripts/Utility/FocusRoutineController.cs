using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 各ルーチンの優先度です
/// 値の低いものから優先度が高くなります(ただしNONEは無効)
/// </summary>
public enum FocusRoutinePriority
{
    NONE = -1,
    TUTORIAL,
    EVENT,
    BATTLE,
}

/// <summary>
/// FocusRoutineをその優先度毎に制御するクラス
/// </summary>
public class FocusRoutineController
{
    IFocusRoutine _currentRoutine = null;
    IFocusRoutine[] _routines = new IFocusRoutine[(int)FocusRoutinePriority.BATTLE + 1];

    public void Init( IFocusRoutine routine )
    {
        _currentRoutine = routine;
    }   

    public void Update()
    {
        int currentPriority = _currentRoutine.GetPriority();

        for (int i = 0; i < _routines.Length; i++)
        {
            if (_routines[i] == null) continue;
            if ( i < currentPriority )
            {
                if (_routines[i].IsRunning())
                {
                    _currentRoutine.Exit();
                    _currentRoutine = _routines[i];
                    _currentRoutine.Restart();
                    return;
                }
            }
        }

        if (_currentRoutine.IsRunning())
        {
            _currentRoutine.Update();
        }
    }
}