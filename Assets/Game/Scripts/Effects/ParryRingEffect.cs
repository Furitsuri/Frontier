using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier
{
    public class ParryRingEffect : MonoBehaviour
    {
        private bool _enabled;
        private bool _isStopShrink;
        private int _judgeRingOuterRadiusID;
        private int _judgeRingInnerRadiusID;
        private int _shrinkRingSizeID;
        private int _shrinkWidthID;
        private float _initTime;
        private float _curtime;
        private Material _ringMaterial;

        void Awake()
        {
            _ringMaterial = Resources.Load<Material>("Materials/Shader/ParryRingShader");
            Debug.Assert(_ringMaterial != null);
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (_ringMaterial != null && _enabled)
            {
                Graphics.Blit(source, destination, _ringMaterial);
            }
            else
            {
                Graphics.Blit(source, destination);
            }
        }

        void OnDestroy()
        {
            Destroy(this);
        }

        /// <summary>
        /// ���������܂�
        /// </summary>
        public void Init( float shrinkTime )
        {
            _judgeRingInnerRadiusID = Shader.PropertyToID("_JudgeRingInnerRadius");
            _judgeRingOuterRadiusID = Shader.PropertyToID("_JudgeRingOuterRadius");
            _shrinkRingSizeID = Shader.PropertyToID("_ShrinkRingSizeRate");
            _shrinkWidthID = Shader.PropertyToID("_ShrinkRingWidth");

            _ringMaterial.SetFloat(_shrinkRingSizeID, 1f);

            _enabled = false;
            _isStopShrink = false;
            _initTime = _curtime = shrinkTime;
        }

        /// <summary>
        /// �j�����܂�
        /// </summary>
        public void Destroy()
        {
            OnDestroy();
        }

        /// <summary>
        /// �G�t�F�N�g���X�V���܂�
        /// </summary>
        public void FixedUpdateEffect()
        {
            if (_isStopShrink) return;

            _curtime -= Time.fixedDeltaTime;
            if (_curtime < 0f) { _curtime = _initTime; }
            // ���Ԕ䗦���T�C�Y���[�g�Ƃ��Ďw��
            UpdateShrinkRingSizeByRate( _curtime / _initTime );
        }

        /// <summary>
        /// �k�����郊���O�̃T�C�Y���[�g���X�V���܂�
        /// </summary>
        /// <param name="sizeRate">�X�V���郊���O�̃T�C�Y���[�g</param>
        public void UpdateShrinkRingSizeByRate(float sizeRate)
        {
            _ringMaterial.SetFloat(_shrinkRingSizeID, sizeRate);
        }

        /// <summary>
        /// �����O�k�����~���܂�
        /// </summary>
        public void StopShrink()
        {
            _isStopShrink = true;
        }

        /// <summary>
        /// �����O�̔��a���擾���܂�
        /// </summary>
        /// <returns>�����O�̔��a</returns>
        public float GetCurShrinkRingRadius()
        {
            return _ringMaterial.GetFloat(_shrinkRingSizeID);
        }

        /// <summary>
        /// �L���E�����ݒ���s���܂�
        /// </summary>
        /// <param name="enable">�L���E����</param>
        public void SetEnable(bool enable)
        {
            _enabled = enable;
        }

        /// <summary>
        /// ���胊���O�͈̔͂�ݒ肵�܂�
        /// </summary>
        /// <param name="outer">���胊���O�O�����a</param>
        /// <param name="innner">���胊���O�������a</param>
        public void SetJudgeRingRange((float inner, float outer) range)
        {
            _ringMaterial.SetFloat(_judgeRingInnerRadiusID, range.inner);
            _ringMaterial.SetFloat(_judgeRingOuterRadiusID, range.outer);
        }
    }
}