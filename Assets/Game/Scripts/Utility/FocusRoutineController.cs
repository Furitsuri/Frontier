using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 各ルーチンの優先度です
/// 値の低いものから優先度が高くなります(ただしNONEは無効)
/// </summary>
public enum FocusRoutinePriority
{
    NONE    = -1,

    DEBUG_EDITOR,   // デバッグエディター
    DEBUG_MENU,     // デバッグメニュー
    TUTORIAL,       // チュートリアル
    EVENT,          // イベント
    BATTLE,         // 戦闘

    NUM,
}

/// <summary>
/// FocusRoutineをその優先度毎に制御するクラス
/// </summary>
public class FocusRoutineController
{
    IFocusRoutine _currentRoutine = null;
    IFocusRoutine[] _routines = new IFocusRoutine[(int)FocusRoutinePriority.NUM];

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

        _currentRoutine.Update();

        UpdateRoutineIfNextIsDue();
    }

    /// <summary>
    /// ルーチンを優先度を指定した上で登録します
    /// </summary>
    /// <param name="routine">登録するルーチン</param>
    /// <param name="p">登録するルーチンの優先度</param>
    public void Register(IFocusRoutine routine, int p )
    {
        if (p < 0 || (int)FocusRoutinePriority.NUM <= p || _routines[p] != null) return;
        _routines[p] = routine;
    }

    /// <summary>
    /// 指定のルーチンを駆動させると共に、駆動中の他のルーチンを中断します
    /// </summary>
    /// <param name="priority">駆動するルーチンの優先度</param>
    public void RunRoutineAndPauseOthers(FocusRoutinePriority priority)
    {
        int p = (int)priority;
        if (p < 0 || (int)FocusRoutinePriority.NUM <= p || _routines[p] == null)
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
    /// 優先して実行されるべきルーチンの存在を確認し、存在する場合は現在のルーチンを中断して新しいルーチンを実行します
    /// </summary>
    private void UpdateRoutineIfNextIsDue()
    {
        int currentPriority = _currentRoutine.GetPriority();

        if( _currentRoutine.IsMatchFocusState( FocusState.EXIT_SCHEDULED ) )
        {
            // 現在のルーチンが終了予定の場合は、現在のルーチンを終了します
            _currentRoutine.Exit();
        }

        // ルーチンの優先度を確認し、優先度が高いものがあれば中断します
        for (int i = 0; i < _routines.Length; i++)
        {
            if (_routines[i] == null || _routines[i].IsMatchFocusState(FocusState.EXIT)) continue;

            // 現在のルーチンより優先度の高いものに対する判定
            if (i < currentPriority)
            {
                // 現在のルーチンが実行中で優先度の高いルーチンが実行予定の場合は、中断して新たに実行
                if (_routines[i].IsMatchFocusState(FocusState.RUN_SCHEDULED))
                {
                    if (_currentRoutine.IsMatchFocusState(FocusState.RUN))
                    {
                        _currentRoutine.Pause();
                    }
                    
                    _currentRoutine = _routines[i];
                    _currentRoutine.Run();

                    return;
                }
            }
            // 現在のルーチンより優先度の低いものに対する判定
            else
            {
                // 現在のルーチンより優先度が高いルーチンが予約されておらず、かつ、現在のルーチンの実行を継続する場合はスルー
                if (_currentRoutine.IsMatchFocusState(FocusState.RUN)) return;

                // 現在のルーチンについては判定しない(RUN以外が指定されている場合)
                if (currentPriority == i) continue;

                // 中断中のルーチンのうち、現在のルーチンの次に優先度が高いものを再開
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