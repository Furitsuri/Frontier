using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace Frontier.UI
{
    /// <summary>
    /// シーン遷移時に表示する暗転（ローディング）画面を管理するシングルトン。
    /// 最初に呼び出されたシーンで生成され、DontDestroyOnLoad で全シーンを跨いで生存します。
    /// </summary>
    [DefaultExecutionOrder( -1000 )]
    public class LoadingScreenController : MonoBehaviour
    {
        private const string ConfigResourcePath = "Config/LoadingScreenResourceConfig";
        private const string LoadingMessage      = "NOW LOADING";
        private const float  PulseCycleDuration  = 1.5f; // 暗→明→暗 一往復にかかる時間(秒)
        private const float  MinAlpha            = 0.2f;
        private const float  MaxAlpha            = 1.0f;

        public static LoadingScreenController Instance { get; private set; }

        [SerializeField] private CanvasGroup _canvasGroup = null;
        [SerializeField] private TMP_Text     _loadingText = null;

        private Coroutine _fadeCoroutine        = null;
        private Coroutine _loadingTextCoroutine = null;

        /// <summary>
        /// インスタンスが存在しない場合は Resources 配下の参照アセット経由でプレハブを取得し生成します。
        /// プレハブ本体は Assets/Game/Prefabs 以下に置き、Resources には参照アセットのみを置く構成です。
        /// シーン開始時に必ず呼び出すことで、起動シーンに依らずローディング画面の存在を保証します。
        /// </summary>
        public static LoadingScreenController EnsureInstance()
        {
            if ( Instance != null ) return Instance;

            var config = Resources.Load<LoadingScreenResourceConfig>( ConfigResourcePath );
            if ( config == null || config.Prefab == null )
            {
                Debug.LogError( $"[LoadingScreenController] 参照アセットまたはプレハブが見つかりません: Resources/{ConfigResourcePath}" );
                return null;
            }

            var instance = Instantiate( config.Prefab );
            instance.name = config.Prefab.name;
            return instance;
        }

        private void Awake()
        {
            if ( Instance == null )
            {
                Instance = this;
                DontDestroyOnLoad( gameObject );
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
                gameObject.SetActive( false );
            }
            else if ( Instance != this )
            {
                Destroy( gameObject );
            }
        }

        private void OnDestroy()
        {
            if ( Instance == this ) Instance = null;
        }

        // 表示中(SetActive(true))の間だけ「NOW LOADING」のアルファ値を滑らかに変化させ続ける
        private void OnEnable()
        {
            if ( _loadingText == null ) return;
            _loadingText.text = LoadingMessage;
            _loadingTextCoroutine = StartCoroutine( AnimateLoadingTextRoutine() );
        }

        private void OnDisable()
        {
            if ( _loadingTextCoroutine != null )
            {
                StopCoroutine( _loadingTextCoroutine );
                _loadingTextCoroutine = null;
            }
            if ( _loadingText != null )
            {
                _loadingText.text = string.Empty;
                _loadingText.alpha = MaxAlpha;
            }
        }

        private IEnumerator AnimateLoadingTextRoutine()
        {
            float elapsed = 0f;
            while ( true )
            {
                elapsed += Time.unscaledDeltaTime;
                // sin波を 0~1 に正規化して MinAlpha~MaxAlpha の範囲でなめらかに往復させる
                float t = ( Mathf.Sin( elapsed / PulseCycleDuration * Mathf.PI * 2f ) + 1f ) * 0.5f;
                _loadingText.alpha = Mathf.Lerp( MinAlpha, MaxAlpha, t );
                yield return null;
            }
        }

        /// <summary>
        /// ローディング画面をフェードインで表示します。
        /// </summary>
        public void Show( float fadeDuration = 0.2f, Action onComplete = null )
        {
            gameObject.SetActive( true );
            _canvasGroup.blocksRaycasts = true;
            StartFade( 1f, fadeDuration, onComplete );
        }

        /// <summary>
        /// ローディング画面をフェードアウトで隠します。
        /// </summary>
        public void Hide( float fadeDuration = 0.2f, Action onComplete = null )
        {
            // すでに非表示の場合はコルーチンを開始できない(非アクティブなGameObject)ため何もしない
            if ( !gameObject.activeSelf )
            {
                onComplete?.Invoke();
                return;
            }

            StartFade( 0f, fadeDuration, () =>
            {
                _canvasGroup.blocksRaycasts = false;
                gameObject.SetActive( false );
                onComplete?.Invoke();
            } );
        }

        private void StartFade( float targetAlpha, float duration, Action onComplete )
        {
            if ( _fadeCoroutine != null ) StopCoroutine( _fadeCoroutine );
            _fadeCoroutine = StartCoroutine( FadeRoutine( targetAlpha, duration, onComplete ) );
        }

        // シーン遷移中は TimeScale が変化している可能性があるため unscaledDeltaTime を使用する
        private IEnumerator FadeRoutine( float targetAlpha, float duration, Action onComplete )
        {
            float startAlpha = _canvasGroup.alpha;

            if ( duration <= 0f )
            {
                _canvasGroup.alpha = targetAlpha;
            }
            else
            {
                float elapsed = 0f;
                while ( elapsed < duration )
                {
                    elapsed += Time.unscaledDeltaTime;
                    _canvasGroup.alpha = Mathf.Lerp( startAlpha, targetAlpha, elapsed / duration );
                    yield return null;
                }
                _canvasGroup.alpha = targetAlpha;
            }

            _fadeCoroutine = null;
            onComplete?.Invoke();
        }
    }
}
