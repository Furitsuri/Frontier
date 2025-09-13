using System.Runtime.CompilerServices;
using UnityEngine;

static public class LogHelper
{
    static public void LogError(
        string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        string className = System.IO.Path.GetFileNameWithoutExtension(filePath);
        Debug.LogError($"[{className}.{memberName} @Line {lineNumber}] {message}");
    }
}