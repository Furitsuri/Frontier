using System;
using System.Diagnostics;
using UnityEngine;
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
}
