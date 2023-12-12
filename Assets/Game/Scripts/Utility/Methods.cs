using System;
using System.Diagnostics;
using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using static UnityEditor.PlayerSettings;

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

    /// <summary>
    /// �ΏۂɎw��̃r�b�g�t���O��ݒ肵�܂�
    /// </summary>
    /// <typeparam name="T">Enum</typeparam>
    /// <param name="flags">�ΏۂƂ���t���O</param>
    /// <param name="value">�w�肷��r�b�g�l</param>
    public static void SetBitFlag<T>(ref T flags, T value) where T : Enum
    {
        int flagsValue = Convert.ToInt32(flags);
        int valueInt = Convert.ToInt32(value);
        flagsValue |= valueInt;
        flags = (T)Enum.ToObject(typeof(T), flagsValue);
    }

    /// <summary>
    /// �Ώۂ̎w��̃r�b�g�t���O���������܂�
    /// </summary>
    /// <typeparam name="T">Enum</typeparam>
    /// <param name="flags">�ΏۂƂ���t���O</param>
    /// <param name="value">�w�肷��r�b�g�l</param>
    public static void UnsetBitFlag<T>(ref T flags, T value) where T : Enum
    {
        int flagsValue = Convert.ToInt32(flags);
        int valueInt = Convert.ToInt32(value);
        flagsValue &= ~valueInt;
        flags = (T)Enum.ToObject(typeof(T), flagsValue);
    }

    /// <summary>
    /// �ΏۂɎw��̃r�b�g�t���O���ݒ肳��Ă��邩���`�F�b�N���܂�
    /// </summary>
    /// <typeparam name="T">Enum</typeparam>
    /// <param name="flags">�ΏۂƂ���t���O</param>
    /// <param name="value">�w�肷��r�b�g�l</param>
    /// <returns>�ݒ肳��Ă��邩�ۂ�</returns>
    public static bool CheckBitFlag<T>(in T flags, T value) where T : Enum
    {
        int flagsValue = Convert.ToInt32(flags);
        int valueInt = Convert.ToInt32(value);
        return 0 != (flagsValue & valueInt);
    }

    /// <summary>
    /// �Ώۂ̃r�b�g�t���O�̑S�Ă̐ݒ���������܂�
    /// </summary>
    /// <typeparam name="T">Enum</typeparam>
    /// <param name="flags">�ΏۂƂ���t���O</param>
    public static void ClearBitFlag<T>(ref T flags) where T : Enum
    {
        int flagsValue = Convert.ToInt32(flags);
        flagsValue &= ~flagsValue; // flagsValue = 0�ł����Ȃ�
        flags = (T)Enum.ToObject(typeof(T), flagsValue);
    }

    /// <summary>
    /// �w��̃x�N�g������]�������l���擾���܂�
    /// </summary>
    /// <param name="baseTransform">��Ƃ���Transform. nulk�̏ꍇ��Vector3�̃f�t�H���g�����g�p</param
    /// <param name="roll">��]�����郍�[���p�x(Degree)</param>
    /// <param name="pitch">��]������s�b�`�p�x(Degree)</param>
    /// <param name="yaw">��]�����郈�[�p�x(Degree)</param>
    /// <param name="vec">��]����x�N�g��</param>
    /// <returns>��]���ʂ̃x�N�g��</returns>
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

        // MEMO : vec�̒l�ɂ���Č������ς��\�������邽�߁AEuler�ł͂Ȃ�AngleAxis��p����
        var rotateQuat = Quaternion.AngleAxis(roll, rollAxis) * Quaternion.AngleAxis(pitch, rightAxis) * Quaternion.AngleAxis(yaw, yawAxis);
        return rotateQuat * vec;
    }

    /// <summary>
    /// ��薡���ɋ߂��L�����N�^�[���r���ĕԂ��܂�
    /// </summary>
    /// <param name="former">�O��</param>
    /// <param name="latter">���</param>
    /// <returns>�O�҂ƌ�҂̂����A��薡���ɋ߂���</returns>
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
