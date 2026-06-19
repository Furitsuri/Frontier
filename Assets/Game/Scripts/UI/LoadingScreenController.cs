using System;
using System.Collections;
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
        private const string PrefabResourcePath = "UI/LoadingScreenController";

        public static LoadingScreenController Instance { get; private set; }

        [SerializeField] private CanvasGroup _canvasGroup = null;

        private Coroutine _fadeCoroutine = null;

        /// <summary>
        /// インスタンスが存在しない場合は Resources からプレハブを生成して確保します。
        /// シーン開始時に必ず呼び出すことで、起動シーンに依らずローディング画面の存在を保証します。
        /// </summary>
        public static LoadingScreenController EnsureInstance()
        {
            if ( Instance != null ) return Instance;

            var prefab = Resources.Load<LoadingScreenController>( PrefabResourcePath );
            if ( prefab == null )
            {
                Debug.LogError( $"[LoadingScreenController] プレハブが見つかりません: Resources/{PrefabResourcePath}" );
                return null;
            }

            var instance = Instantiate( prefab );
            instance.name = prefab.name;
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
