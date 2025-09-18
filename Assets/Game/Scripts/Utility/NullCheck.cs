using System;
using UnityEngine;

/// <summary>
/// Nullチェックに使用する例外処理用クラスです
/// </summary>
static public class NullCheck
{
    static public void AssertNotNull(object obj, string paramName)
    {
        if (obj == null)
        {
            Debug.Assert(false, $"{paramName} should not be null.");
            throw new ArgumentNullException(paramName);
        }
    }
}