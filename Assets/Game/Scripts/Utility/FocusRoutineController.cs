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

    MAX,
}

/// <summary>
/// FocusRoutineをその優先度毎に制御するクラス
/// </summary>
public class FocusRoutineController
{
    IFocusRoutine _currentRoutine = null;
    IFocusRoutine[] _routines = new IFocusRoutine[(int)FocusRoutinePriority.BATTLE + 1];

    /// <summary>
    /// 初期化します
    /// </summary>
    public void Init()
    {
        _currentRoutine = null;

        for (int i = 0; i < _routines.Length; i++)
        {
            if (_routines[i] != null)
            {
                _routines[i].Exit();
            }
            _routines[i] = null;
        }
    }

    /// <summary>
    /// 優先度の高いルーチンが実行されている場合は、現在のルーチンを中断して新しいルーチンを実行します
    /// </summary>
    public void Update()
    {
        if (null == _currentRoutine)
        {
            LogHelper.LogError("Current routine is null.");
            return;
        }

        UpdateRoutineIfNextIsDue();
    }

    /// <summary>
    /// ルーチンを優先度を指定した上で登録します
    /// </summary>
    /// <param name="routine">登録するルーチン</param>
    /// <param name="priority">登録するルーチンの優先度</param>
    public void Register(IFocusRoutine routine, int priorityIdx )
    {
        int p = (int)priorityIdx;

        if (p < 0 || (int)FocusRoutinePriority.MAX <= p || _routines[p] != null) return;
        _routines[p] = routine;
    }

    /// <summary>
    /// 指定のルーチンを駆動させると共に、駆動中の他のルーチンを中断します
    /// </summary>
    /// <param name="priority">駆動するルーチンの優先度</param>
    public void RunRoutineAndPauseOthers(FocusRoutinePriority priority)
    {
        int p = (int)priority;
        if (p < 0 || (int)FocusRoutinePriority.MAX <= p || _routines[p] == null)
        {
            LogHelper.LogError("Invalid priority");
            return;
        }

        for( int i = 0; i < _routines.Length; ++i )
        {
            if (_routines[i] == null)
            {
                continue;
            }

            if (i == p)
            {
                _currentRoutine = _routines[p];
                _currentRoutine.Run();
                continue;
            }

            if (_routines[i].IsMatchFocusState( FocusState.RUN ))
            {
                _routines[i].Pause();
            }
        }
    }

    /// <summary>
    /// 次のルーチンが実行されるべきかを確認し、実行されるべきであれば現在のルーチンを中断して新しいルーチンを実行します
    /// </summary>
    private void UpdateRoutineIfNextIsDue()
    {
        int currentPriority = _currentRoutine.GetPriority();

        // ルーチンの優先度を確認し、優先度が高いものがあれば中断します
        for (int i = 0; i < _routines.Length; i++)
        {
            if (_routines[i] == null) continue;

            if (i < currentPriority)
            {
                if (_routines[i].IsMatchFocusState(FocusState.RESERVE))
                {
                    // 現在のルーチンを中断します
                    _currentRoutine.Pause();
                    _currentRoutine = _routines[i];

                    // 中断したルーチンの場合は、再開します
                    if (_routines[i].IsMatchFocusState(FocusState.PAUSE))
                    {
                        _currentRoutine.Restart();
                    }
                    else
                    {
                        _currentRoutine.Run();
                    }

                    return;
                }
            }
            else
            {
                // 現在のルーチンより優先度が高いルーチンが予約されておらず、かつ、現在のルーチンの実行を継続する場合はスルーします
                if (_currentRoutine.IsMatchFocusState(FocusState.RUN)) return;

                // 中断中のルーチンの中で、現在のルーチンの次に優先度が高いものを再開します
                if (_routines[i].IsMatchFocusState(FocusState.PAUSE))
                {
                    _currentRoutine = _routines[i];
                    _currentRoutine.Restart();

                    return;
                }
            }
        }

        LogHelper.LogError("実行すべきルーチンが存在していない状態です。");
    }
}