using System;
using UnityEngine;

namespace Frontier.Entities
{
    /// <summary>
    /// �L�����N�^�[�ɑ΂���J�����p�����[�^�̍\���̂ł�
    /// Inspector��ŕҏW�o����ƕ֗��Ȃ��߁A�ʃt�@�C���Ɉڏ����Ă��܂���
    /// </summary>
    [Serializable]
    public struct CameraParameter
    {
        [Header("�U���V�[�P���X���J�����I�t�Z�b�g")]
        public Vector3 OffsetOnAtkSequence;
        [Header("�p�����[�^�\��UI�p�J�����I�t�Z�b�g(Y���W)")]
        public float UICameraLengthY;
        [Header("�p�����[�^�\��UI�p�J�����I�t�Z�b�g(Z���W)")]
        public float UICameraLengthZ;
        // UI�\���p�J�����^�[�Q�b�g(Y����)
        public float UICameraLookAtCorrectY;

        public CameraParameter(in Vector3 offset, float lengthY, float lengthZ, float lookAtCorrectY)
        {
            OffsetOnAtkSequence = offset;
            UICameraLengthY = lengthY;
            UICameraLengthZ = lengthZ;
            UICameraLookAtCorrectY = lookAtCorrectY;
        }
    }
}