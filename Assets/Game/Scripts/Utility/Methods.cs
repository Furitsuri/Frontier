using Frontier.Entities;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Text.Json;
using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

static public class Methods
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    static public void Dispose<T>( T obj ) where T : class
    {
        if( obj == null ) { return; }

        if( obj is IDisposer disposer )
        {
            disposer.Dispose();
        }

        obj = null;
    }

    /// <summary>
    /// 自分自身を含むすべての子オブジェクトのレイヤーを設定します
    /// </summary>
    /// <param name="self">自身</param>
    /// <param name="layer">指定レイヤー</param>
    static public void SetLayerRecursively(this GameObject self, int layer)
    {
        self.layer = layer;

        foreach (Transform n in self.transform)
        {
            SetLayerRecursively(n.gameObject, layer);
        }
    }

    /// <summary>
    /// 対象に指定のビットフラグを設定します
    /// </summary>
    /// <typeparam name="T">Enum</typeparam>
    /// <param name="flags">対象とするフラグ</param>
    /// <param name="value">指定するビット値</param>
    static public void SetBitFlag<T>( ref int flags, T value ) where T : Enum
    {
        flags           |= ToBit( value );
    }

    static public void SetBitFlag<T>( ref Int64 flags, T value ) where T : Enum
    {
        flags |= ToBit64( value );
    }

    /// <summary>
    /// 対象に指定のビットフラグを設定します
    /// </summary>
    /// <typeparam name="T">Enum</typeparam>
    /// <param name="flags">対象とするフラグ</param>
    /// <param name="value">指定するビット値</param>
    static public void SetBitFlag<T>(ref T flags, T value) where T : Enum
    {
        int flagsValue  = Convert.ToInt32(flags);
        int valueInt    = Convert.ToInt32(value);
        flagsValue      |= valueInt;
        flags           = (T)Enum.ToObject(typeof(T), flagsValue);
    }

    /// <summary>
    /// 対象のビットフラグの全ての設定を解除します
    /// </summary>
    /// <typeparam name="T">Enum</typeparam>
    /// <param name="flags">対象とするフラグ</param>
    static public void ClearBitFlag<T>( ref int flags )
    {
        flags = 0;
    }

    /// <summary>
    /// 対象のビットフラグの全ての設定を解除します
    /// </summary>
    /// <typeparam name="T">Enum</typeparam>
    /// <param name="flags">対象とするフラグ</param>
    static public void ClearBitFlag<T>( ref T flags ) where T : Enum
    {
        int flagsValue  = Convert.ToInt32(flags);
        flagsValue      &= ~flagsValue; // flagsValue = 0でも問題ない
        flags           = ( T )Enum.ToObject( typeof( T ), flagsValue );
    }

    /// <summary>
    /// 対象の指定のビットフラグを解除します
    /// </summary>
    /// <typeparam name="T">Enum</typeparam>
    /// <param name="flags">対象とするフラグ</param>
    /// <param name="value">指定するビット値</param>
    static public void UnsetBitFlag<T>( ref int flags, T value ) where T : Enum
    {
        flags &= ~( ToBit( value ) );
    }

    static public void UnsetBitFlag<T>( ref Int64 flags, T value ) where T : Enum
    {
        flags &= ~( ToBit64( value ) );
    }

    /// <summary>
    /// 対象の指定のビットフラグを解除します
    /// </summary>
    /// <typeparam name="T">Enum</typeparam>
    /// <param name="flags">対象とするフラグ</param>
    /// <param name="value">指定するビット値</param>
    static public void UnsetBitFlag<T>(ref T flags, T value) where T : Enum
    {
        int flagsValue  = Convert.ToInt32(flags);
        int valueInt    = Convert.ToInt32(value);
        flagsValue      &= ~valueInt;
        flags           = (T)Enum.ToObject(typeof(T), flagsValue);
    }

    /// <summary>
    /// 対象に指定のビットフラグが設定されているかをチェックします
    /// </summary>
    /// <typeparam name="T">Enum</typeparam>
    /// <param name="flags">対象とするフラグ</param>
    /// <param name="value">指定するビット値</param>
    /// <returns>設定されているか否か</returns>
    static public bool HasAnyFlag<T>( in int flags, T value ) where T : Enum
    {
        return 0 != ( flags & ToBit( value ) );
    }

    /// <summary>
    /// 対象に指定のビットフラグが設定されているかをチェックします
    /// </summary>
    /// <typeparam name="T">Enum</typeparam>
    /// <param name="flags">対象とするフラグ</param>
    /// <param name="value">指定するビット値</param>
    /// <returns>設定されているか否か</returns>
    static public bool HasAnyFlag<T>( in T flags, T value ) where T : Enum
    {
        int flagsValue  = Convert.ToInt32( flags );
        int valueInt    = Convert.ToInt32( value );
        return 0 != ( flagsValue & valueInt );
    }

    static public bool HasAllFlags<T>( T flags, T value ) where T : Enum
    {
        int flagsValue  = Convert.ToInt32( flags );
        int valueInt    = Convert.ToInt32( value );
        return ( flagsValue & valueInt ) == valueInt;
    }

    static public bool IsMatchForward( in Vector3 baseForward, in Vector3 basePos, in Vector3 targetPos )
    {
        var direction   = targetPos - basePos;
        direction.y     = 0f;
        direction       = direction.normalized;

        return Constants.DOT_THRESHOLD < Vector3.Dot( baseForward, direction );
    }

    /// <summary>
    /// 対象のenum値をビット値に変換します
    /// </summary>
    /// <typeparam name="T">enum値の型</typeparam>
    /// <param name="value">enum値</param>
    /// <returns>変換されたビット値</returns>
    static public int ToBit<T>( this T value ) where T : Enum
    {
        int v = Convert.ToInt32( value );
        if( v < 0 ) { return 0; }

        return 1 << v;
    }

    static public Int64 ToBit64<T>( this T value ) where T : Enum
    {
        int v = Convert.ToInt32( value );
        if( v < 0 ) { return 0; }

        return 1L << v;
    }

    /// <summary>
    /// 指定の配列内の全ての要素が条件を満たすかをチェックします
    /// </summary>
    /// <typeparam name="T">指定配列の型</typeparam>
    /// <param name="array">指定配列</param>
    /// <param name="predicate">条件</param>
    /// <returns>指定の条件全てを満たしているか</returns>
    static public bool AllMatch<T>( T[] array, Func<T, bool> predicate )
    {
        if ( array == null || predicate == null ) return false;

        foreach ( var item in array )
        {
            if ( !predicate( item ) )
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 指定のベクトルを回転させた値を取得します
    /// </summary>
    /// <param name="baseTransform">基軸とするTransform. nullの場合はVector3のデフォルト軸を使用</param>
    /// <param name="roll">回転させるロール角度(Degree)</param>
    /// <param name="pitch">回転させるピッチ角度(Degree)</param>
    /// <param name="yaw">回転させるヨー角度(Degree)</param>
    /// <param name="vec">回転するベクトル</param>
    /// <returns>回転結果のベクトル</returns>
    static public Vector3 RotateVector( in Transform baseTransform, float pitch, float yaw, float roll, in Vector3 vec )
    {
        Vector3 rollAxis, pitchAxis, yawAxis;

        if( baseTransform == null )
        {
            rollAxis    = Vector3.forward;
            pitchAxis = Vector3.right;
            yawAxis     = Vector3.up;
        }
        else
        {
            rollAxis    = baseTransform.forward;
            pitchAxis   = baseTransform.right;
            yawAxis     = baseTransform.up;
        }

        Quaternion yawRot   = Quaternion.AngleAxis( yaw, Vector3.up );
        Quaternion pitchRot = Quaternion.AngleAxis( pitch, Vector3.right );
        Quaternion rollRot  = Quaternion.AngleAxis( roll, Vector3.forward );

        // MEMO : vecの値によって向きが変わる可能性があるため、EulerではなくAngleAxisを用いる
        var rotateQuat = Quaternion.AngleAxis( roll, rollAxis ) * Quaternion.AngleAxis( pitch, pitchAxis ) * Quaternion.AngleAxis( yaw, yawAxis );
        return rotateQuat * vec;
    }

    /// <summary>
    /// より味方に近いキャラクターを比較して返します
    /// </summary>
    /// <param name="former">前者</param>
    /// <param name="latter">後者</param>
    /// <returns>前者と後者のうち、より味方に近い側</returns>
    static public Character CompareAllyCharacter( Character former, Character latter )
    {
        var formerTag = former.GetStatusRef.characterTag;
        var latterTag = latter.GetStatusRef.characterTag;

        if ( formerTag != CHARACTER_TAG.PLAYER )
        {
            if( latterTag == CHARACTER_TAG.PLAYER )
            {
                return latter;
            }
            else
            {
                if( formerTag == CHARACTER_TAG.OTHER )
                {
                    return former;
                }
                else
                {
                    return latter;
                }
            }
        }

        return former;
    }

    /// <summary>
    /// 指定されたベクトルをワールド座標からEnum値Directionに変換します
    /// </summary>
    /// <param name=""></param>
    /// <returns></returns>
    static public Direction ConvertDirectionFromVector( in Vector3 vec )
    {
        float dotForward    = Vector3.Dot( vec, Vector3.forward ); // ワールド前(Z+)
        float dotRight      = Vector3.Dot( vec, Vector3.right );   // ワールド右(X+)

        if( dotForward > 0.7f )
        {
            return Direction.FORWARD; // 前
        }
        else if( dotForward < -0.7f )
        {
            return Direction.BACK;  // 後ろ
        }
        else if( dotRight > 0.7f )
        {
            return Direction.RIGHT; // 右
        }
        else if( dotRight < -0.7f )
        {
            return Direction.LEFT;  // 左
        }

        return Direction.NONE;
    }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
    static public bool IsDebugScene()
    {
        var curSceneName = SceneManager.GetActiveScene().name;
        return curSceneName.Contains("Debug");
    }
#endif  // DEVELOPMENT_BUILD || UNITY_EDITOR
}
