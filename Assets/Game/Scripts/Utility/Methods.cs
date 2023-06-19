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
}
