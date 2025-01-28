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
        /// 初期化します
        /// </summary>
        public void Init( float shrinkTime )
        {
            _judgeRingInnerRadiusID = Shader.PropertyToID("_JudgeRingInnerRadius");
            _judgeRingOuterRadiusID = Shader.PropertyToID("_JudgeRingOuterRadius");
            _shrinkRingSizeID       = Shader.PropertyToID("_ShrinkRingSizeRate");
            _shrinkWidthID          = Shader.PropertyToID("_ShrinkRingWidth");

            _ringMaterial.SetFloat(_shrinkRingSizeID, 1f);

            _enabled        = false;
            _isStopShrink   = false;
            _initTime       = _curtime = shrinkTime;
        }

        /// <summary>
        /// 破棄します
        /// </summary>
        public void Destroy()
        {
            OnDestroy();
        }

        /// <summary>
        /// エフェクトを更新します
        /// </summary>
        public void FixedUpdateEffect()
        {
            if (_isStopShrink) return;

            _curtime -= Time.fixedDeltaTime;
            if (_curtime < 0f) { _curtime = _initTime; }
            // 時間比率をサイズレートとして指定
            UpdateShrinkRingSizeByRate( _curtime / _initTime );
        }

        /// <summary>
        /// 縮小するリングのサイズレートを更新します
        /// </summary>
        /// <param name="sizeRate">更新するリングのサイズレート</param>
        public void UpdateShrinkRingSizeByRate(float sizeRate)
        {
            _ringMaterial.SetFloat(_shrinkRingSizeID, sizeRate);
        }

        /// <summary>
        /// リング縮小を停止します
        /// </summary>
        public void StopShrink()
        {
            _isStopShrink = true;
        }

        /// <summary>
        /// リングの半径を取得します
        /// </summary>
        /// <returns>リングの半径</returns>
        public float GetCurShrinkRingRadius()
        {
            return _ringMaterial.GetFloat(_shrinkRingSizeID);
        }

        /// <summary>
        /// 有効・無効設定を行います
        /// </summary>
        /// <param name="enable">有効・無効</param>
        public void SetEnable(bool enable)
        {
            _enabled = enable;
        }

        /// <summary>
        /// 判定リングの範囲を設定します
        /// </summary>
        /// <param name="outer">判定リング外周半径</param>
        /// <param name="innner">判定リング内周半径</param>
        public void SetJudgeRingRange((float inner, float outer) range)
        {
            _ringMaterial.SetFloat(_judgeRingInnerRadiusID, range.inner);
            _ringMaterial.SetFloat(_judgeRingOuterRadiusID, range.outer);
        }
    }
}