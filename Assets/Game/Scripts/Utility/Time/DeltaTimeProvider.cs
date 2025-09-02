using UnityEngine;

/// <summary>
/// DeltaTime を取得するラッパー。
/// デバッグ時は固定値を返すことで、ブレーク時の飛び跳ねを防止。
/// Update と FixedUpdate どちらにも対応。
/// </summary>
public static class DeltaTimeProvider
{
    /// <summary>デバッグ時に固定フレーム時間を使用するか</summary>
    public static bool UseFixedDeltaTimeInDebug = false;

    /// <summary>デバッグ時に返す固定 DeltaTime (60FPS想定)</summary>
    private const float fixedDeltaTime = 0.00058623f;

    /// <summary>Update 用の deltaTime</summary>
    public static float DeltaTime
    {
        get
        {
#if UNITY_EDITOR
            if ( UseFixedDeltaTimeInDebug && Debug.isDebugBuild )
            {
                return fixedDeltaTime;
            }
#endif
            return Time.deltaTime;
        }
    }

    /// <summary>FixedUpdate 用の deltaTime</summary>
    public static float FixedDeltaTime
    {
        get
        {
#if UNITY_EDITOR
            if ( UseFixedDeltaTimeInDebug && Debug.isDebugBuild )
            {
                return fixedDeltaTime;
            }
#endif
            return Time.fixedDeltaTime;
        }
    }
}