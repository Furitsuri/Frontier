using System;
using System.Diagnostics;

public static class DebugUtils
{
    /// <summary>
    /// NullAssert検出時に用います
    /// </summary>
    /// <param name="obj">検出対象のインスタンス</param>
    public static void NULL_ASSERT(object obj)
    { 
        Debug.Assert(obj != null, $"{nameof(obj)} is not assigned.");
    }
}