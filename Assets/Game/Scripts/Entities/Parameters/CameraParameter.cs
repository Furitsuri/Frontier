using System;
using UnityEngine;

namespace Frontier.Entities
{
    /// <summary>
    /// キャラクターに対するカメラパラメータの構造体です
    /// Inspector上で編集出来ると便利なため、別ファイルに移譲していません
    /// </summary>
    [Serializable]
    public struct CameraParameter
    {
        [Header("攻撃シーケンス時カメラオフセット")]
        public Vector3 OffsetOnAtkSequence;
        [Header("パラメータ表示UI用カメラオフセット(Y座標)")]
        public float UICameraLengthY;
        [Header("パラメータ表示UI用カメラオフセット(Z座標)")]
        public float UICameraLengthZ;
        [Header("UI表示用カメラターゲット(Y方向)")]
        public float UICameraLookAtCorrectY;

        /// <summary>
        /// 初期化します
        /// </summary>
        public void Init()
        {
        }

        public CameraParameter(in Vector3 offset, float lengthY, float lengthZ, float lookAtCorrectY)
        {
            OffsetOnAtkSequence     = offset;
            UICameraLengthY         = lengthY;
            UICameraLengthZ         = lengthZ;
            UICameraLookAtCorrectY  = lookAtCorrectY;
        }
    }
}