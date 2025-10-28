using System;
using System.Diagnostics;
using UnityEngine;

/// <summary>
/// Nullチェックに使用する例外処理用クラスです
/// </summary>
static public class NullCheck
{
    [Conditional( "UNITY_EDITOR" )]
    static public void AssertNotNull( object obj, string paramName )
    {
        if( obj == null )
        {
            UnityEngine.Debug.Assert( false, $"{paramName} should not be null." );
            throw new ArgumentNullException( paramName );
        }
    }
}