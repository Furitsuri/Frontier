﻿using System.Runtime.CompilerServices;
using UnityEngine;

public static class LogHelper
{
    public static void LogError(
        string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        string className = System.IO.Path.GetFileNameWithoutExtension(filePath);
        Debug.LogError($"[{className}.{memberName} @Line {lineNumber}] {message}");
    }
}