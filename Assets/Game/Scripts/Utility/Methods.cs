using Frontier.Entities;
using System;
using System.Diagnostics;
using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public static class Methods
{
    /// <summary>
    /// 自分自身を含むすべての子オブジェクトのレイヤーを設定します
    /// </summary>
    /// <param name="self">自身</param>
    /// <param name="layer">指定レイヤー</param>
    public static void SetLayerRecursively(this GameObject self, int layer)
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
    public static void SetBitFlag<T>(ref T flags, T value) where T : Enum
    {
        int flagsValue = Convert.ToInt32(flags);
        int valueInt = Convert.ToInt32(value);
        flagsValue |= valueInt;
        flags = (T)Enum.ToObject(typeof(T), flagsValue);
    }

    /// <summary>
    /// 対象の指定のビットフラグを解除します
    /// </summary>
    /// <typeparam name="T">Enum</typeparam>
    /// <param name="flags">対象とするフラグ</param>
    /// <param name="value">指定するビット値</param>
    public static void UnsetBitFlag<T>(ref T flags, T value) where T : Enum
    {
        int flagsValue = Convert.ToInt32(flags);
        int valueInt = Convert.ToInt32(value);
        flagsValue &= ~valueInt;
        flags = (T)Enum.ToObject(typeof(T), flagsValue);
    }

    /// <summary>
    /// 対象に指定のビットフラグが設定されているかをチェックします
    /// </summary>
    /// <typeparam name="T">Enum</typeparam>
    /// <param name="flags">対象とするフラグ</param>
    /// <param name="value">指定するビット値</param>
    /// <returns>設定されているか否か</returns>
    public static bool CheckBitFlag<T>(in T flags, T value) where T : Enum
    {
        int flagsValue = Convert.ToInt32(flags);
        int valueInt = Convert.ToInt32(value);
        return 0 != (flagsValue & valueInt);
    }

    /// <summary>
    /// 対象のビットフラグの全ての設定を解除します
    /// </summary>
    /// <typeparam name="T">Enum</typeparam>
    /// <param name="flags">対象とするフラグ</param>
    public static void ClearBitFlag<T>(ref T flags) where T : Enum
    {
        int flagsValue = Convert.ToInt32(flags);
        flagsValue &= ~flagsValue; // flagsValue = 0でも問題ない
        flags = (T)Enum.ToObject(typeof(T), flagsValue);
    }

    /// <summary>
    /// 指定のベクトルを回転させた値を取得します
    /// </summary>
    /// <param name="baseTransform">基軸とするTransform. nulkの場合はVector3のデフォルト軸を使用</param
    /// <param name="roll">回転させるロール角度(Degree)</param>
    /// <param name="pitch">回転させるピッチ角度(Degree)</param>
    /// <param name="yaw">回転させるヨー角度(Degree)</param>
    /// <param name="vec">回転するベクトル</param>
    /// <returns>回転結果のベクトル</returns>
    public static Vector3 RotateVector( in Transform baseTransform, float roll, float pitch, float yaw, in Vector3 vec )
    {
        Vector3 rollAxis, rightAxis, yawAxis;

        if( baseTransform == null )
        {
            rollAxis    = Vector3.forward;
            rightAxis   = Vector3.right;
            yawAxis     = Vector3.up;
        }
        else
        {
            rollAxis    = baseTransform.forward;
            rightAxis   = baseTransform.right;
            yawAxis     = baseTransform.up;
        }

        // MEMO : vecの値によって向きが変わる可能性があるため、EulerではなくAngleAxisを用いる
        var rotateQuat = Quaternion.AngleAxis(roll, rollAxis) * Quaternion.AngleAxis(pitch, rightAxis) * Quaternion.AngleAxis(yaw, yawAxis);
        return rotateQuat * vec;
    }

    /// <summary>
    /// より味方に近いキャラクターを比較して返します
    /// </summary>
    /// <param name="former">前者</param>
    /// <param name="latter">後者</param>
    /// <returns>前者と後者のうち、より味方に近い側</returns>
    public static Character CompareAllyCharacter( Character former, Character latter )
    {
        var formerTag = former.characterParam.characterTag;
        var latterTag = latter.characterParam.characterTag;

        if ( formerTag != Character.CHARACTER_TAG.PLAYER )
        {
            if( latterTag == Character.CHARACTER_TAG.PLAYER )
            {
                return latter;
            }
            else
            {
                if( formerTag == Character.CHARACTER_TAG.OTHER )
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

#if DEVELOPMENT_BUILD || UNITY_EDITOR
    public static bool IsDebugScene()
    {
        var curSceneName = SceneManager.GetActiveScene().name;
        return curSceneName.Contains("Debug");
    }
#endif  // DEVELOPMENT_BUILD || UNITY_EDITOR
}
