using System.Diagnostics;
using UnityEngine;
using UnityEngine.UIElements;

public static class Methods
{
    /// <summary>
    /// �������g���܂ނ��ׂĂ̎q�I�u�W�F�N�g�̃��C���[��ݒ肵�܂�
    /// </summary>
    /// <param name="self">���g</param>
    /// <param name="layer">�w�背�C���[</param>
    public static void SetLayerRecursively(this GameObject self, int layer)
    {
        self.layer = layer;

        foreach (Transform n in self.transform)
        {
            SetLayerRecursively(n.gameObject, layer);
        }
    }
}
