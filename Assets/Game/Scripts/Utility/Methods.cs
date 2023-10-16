using System;
using System.Diagnostics;
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
    /// より味方に近いキャラクターを比較して返します
    /// </summary>
    /// <param name="former">前者</param>
    /// <param name="latter">後者</param>
    /// <returns>前者と後者のうち、より味方に近い側</returns>
    public static Frontier.Character CompareAllyCharacter( Frontier.Character former, Frontier.Character latter )
    {
        var formerTag = former.param.characterTag;
        var latterTag = latter.param.characterTag;

        if ( formerTag != Frontier.Character.CHARACTER_TAG.PLAYER )
        {
            if( latterTag == Frontier.Character.CHARACTER_TAG.PLAYER )
            {
                return latter;
            }
            else
            {
                if( formerTag == Frontier.Character.CHARACTER_TAG.OTHER )
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
