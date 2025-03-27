using System;
using UnityEngine;

/// <summary>
/// Nullチェックに使用する例外処理用クラスです
/// </summary>
public static class NullCheck
{
    public static void AssertNotNull(object obj)
    {
        if (obj == null)
        {
            string paramName = nameof(obj);
            Debug.Assert(false, $"{paramName} should not be null.");
            throw new ArgumentNullException(paramName);
        }
    }
}