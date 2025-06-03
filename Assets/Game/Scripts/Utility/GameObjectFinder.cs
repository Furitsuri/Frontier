using UnityEngine;
using System.Collections.Generic;

public static class GameObjectFinder
{
    /// <summary>
    /// シーン内の全 GameObject（非アクティブ含む）から名前で探索します。
    /// </summary>
    /// <param name="name">検索したいオブジェクト名</param>
    /// <returns>見つかった GameObject（複数ある場合は最初の1つ）</returns>
    public static GameObject FindInSceneEvenIfInactive(string name)
    {
        foreach (var rootObj in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            var result = FindInChildrenRecursive(rootObj.transform, name);
            if (result != null)
                return result.gameObject;
        }

        return null;
    }

    /// <summary>
    /// Transform を再帰的に探索し、名前に一致する Transform を返します。
    /// </summary>
    private static Transform FindInChildrenRecursive(Transform parent, string name)
    {
        if (parent.name == name)
            return parent;

        foreach (Transform child in parent)
        {
            var result = FindInChildrenRecursive(child, name);
            if (result != null)
                return result;
        }

        return null;
    }
}