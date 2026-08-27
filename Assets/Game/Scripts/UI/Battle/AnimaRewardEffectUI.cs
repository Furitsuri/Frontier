using System;
using UnityEngine;
using UnityEngine.UI;

namespace Frontier.UI
{
    /// <summary>
    /// 敵撃破位置から戦闘中アニマ表示(BattleAnimaUI)へ向けて、白い球エフェクト(大小3つ)が
    /// 吸い込まれるように移動する演出です。Play()で開始し、全ての球が到達した際にonArrivedを呼びます。
    /// 到達までは実際のアニマ加算を行わないため、呼び出し側はonArrived内で加算処理を行ってください。
    /// 球のスプライトはAssets/Game/Textures/UI/AnimaOrb.pngをInspector経由で参照しています
    /// (見た目を調整したい場合はこの画像ファイルを差し替えてください)。
    /// </summary>
    public class AnimaRewardEffectUI : UiMonoBehaviour
    {
        [SerializeField] private Image[] _orbs;

        private RectTransform _btlUiRectTransform;
        private Camera _btlUiCamera;
        private Vector2 _startLocalPos;
        private Vector2 _targetLocalPos;
        private Vector3[] _orbInitialScales;
        private float _elapsed;
        private Action _onArrived;
        private bool _isPlaying;

        /// <summary>
        /// 全ての球が到達しUIへ加算済みかどうか。Play()呼び出しからonArrived直前までtrueです。
        /// </summary>
        public bool IsPlaying => _isPlaying;

        public override void Setup()
        {
            base.Setup();

            _orbInitialScales = new Vector3[_orbs.Length];
            for ( int i = 0; i < _orbs.Length; ++i )
            {
                _orbInitialScales[i] = _orbs[i].rectTransform.localScale;
            }
        }

        /// <summary>
        /// 初期化します
        /// </summary>
        /// <param name="rect">BattleUISystemのRectTransform</param>
        /// <param name="uiCamera">BattleUISystemに用いるUI用カメラ</param>
        public void Init( RectTransform rect, Camera uiCamera )
        {
            _btlUiRectTransform = rect;
            _btlUiCamera        = uiCamera;
        }

        /// <summary>
        /// エフェクトを再生します。
        /// </summary>
        /// <param name="startWorldPos">出発位置(倒した敵のワールド座標)</param>
        /// <param name="targetLocalPos">到達位置(BattleUIのRectTransformを基準としたローカル座標)</param>
        /// <param name="onArrived">全ての球が到達した際に呼ばれるコールバック(実際のアニマ加算はここで行うこと)</param>
        public void Play( Vector3 startWorldPos, Vector2 targetLocalPos, Action onArrived )
        {
            var screenPos = RectTransformUtility.WorldToScreenPoint( Camera.main, startWorldPos );
            RectTransformUtility.ScreenPointToLocalPointInRectangle( _btlUiRectTransform, screenPos, _btlUiCamera, out _startLocalPos );

            _targetLocalPos = targetLocalPos;
            _onArrived       = onArrived;
            _elapsed         = 0f;
            _isPlaying       = true;

            for ( int i = 0; i < _orbs.Length; ++i )
            {
                _orbs[i].rectTransform.localPosition = _startLocalPos;
                _orbs[i].rectTransform.localScale    = _orbInitialScales[i];
            }

            gameObject.SetActive( true );
        }

        void Update()
        {
            if ( !_isPlaying ) { return; }

            _elapsed += DeltaTimeProvider.DeltaTime;

            bool allArrived = true;
            for ( int i = 0; i < _orbs.Length; ++i )
            {
                float startDelay = i * Constants.ANIMA_EFFECT_ORB_STAGGER;
                float t = Mathf.Clamp01( ( _elapsed - startDelay ) / Constants.ANIMA_EFFECT_DURATION );
                if ( t < 1f ) { allArrived = false; }

                float eased = t * t; // 加速しながら吸い込まれる動き
                Vector2 pos = Vector2.Lerp( _startLocalPos, _targetLocalPos, eased );
                pos.y += Mathf.Sin( t * Mathf.PI ) * Constants.ANIMA_EFFECT_ARC_HEIGHT; // 弧を描く軌道

                _orbs[i].rectTransform.localPosition = pos;
                _orbs[i].rectTransform.localScale    = _orbInitialScales[i] * Mathf.Lerp( 1f, 0.2f, eased );
            }

            if ( allArrived )
            {
                _isPlaying = false;
                var callback = _onArrived;
                _onArrived = null;

                Hide();
                callback?.Invoke();
            }
        }

        public void Hide()
        {
            gameObject.SetActive( false );
        }
    }
}
