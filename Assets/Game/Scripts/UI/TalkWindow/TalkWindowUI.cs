using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Frontier.UI
{
    /// <summary>
    /// 話者名・セリフ・ポートレートを表示する汎用トークウィンドウの見た目のみを担当する最小限のビュー。
    /// 表示内容の決定(どのシーンのどの発言を表示するか)は GeneralUISystem.ShowTalk() が行い、
    /// このクラスは受け取った内容を反映するだけに留める。
    /// </summary>
    public class TalkWindowUI : UiMonoBehaviour
    {
        [Header( "話者名" )]
        [SerializeField] private TextMeshProUGUI _speakerNameText;

        [Header( "セリフ本文" )]
        [SerializeField] private TextMeshProUGUI _messageText;

        [Header( "ポートレート画像" )]
        [SerializeField] private Image _portraitImage;

        // 画面右上(ヘッダー直下)に表示する場合の配置。実測して決めた値。
        private static readonly Vector2 TopRightAnchor           = new Vector2( 1f, 1f );
        private static readonly Vector2 TopRightAnchoredPosition = new Vector2( -24f, -65f );
        private const float TopRightWindowWidth = 560f;

        // セリフ本文の上端オフセットと下端マージン(実測値)。ウィンドウ高さの自動計算に用いる
        private const float MessageTextTopOffset    = 45f;
        private const float MessageTextBottomMargin = 10f;

        // 本文が1行のみでも窮屈にならない下限、長文でも画面から破綻しない上限
        private const float MinWindowHeight = 130f;
        private const float MaxWindowHeight = 260f;

        // 既定配置はポートレートとの間隔を確保した幅、右上配置はポートレートを表示しないためウィンドウ幅に
        // 収まる幅を使う(実測値)。右上配置でも既定と同じ620を使うと、ウィンドウより本文欄の方が広くなり
        // 文字がウィンドウ右端からはみ出してしまうため、配置ごとに個別の幅を持たせている
        private const float DefaultMessageTextWidth  = 620f;
        private const float TopRightMessageTextWidth = 510f;

        private RectTransform _rectTransform;
        private RectTransform _messageTextRect;
        private Vector2 _defaultAnchorMin;
        private Vector2 _defaultAnchorMax;
        private Vector2 _defaultPivot;
        private Vector2 _defaultAnchoredPosition;
        private float _defaultWindowWidth;

        private float _currentWindowWidth;
        private float _currentMessageTextWidth;

        public override void Setup()
        {
            base.Setup();

            // prefabに設定済みの位置(雇用/解雇選択のクッション画面用)を既定値として保持しておく
            _rectTransform   = ( RectTransform ) transform;
            _messageTextRect = _messageText.rectTransform;

            _defaultAnchorMin        = _rectTransform.anchorMin;
            _defaultAnchorMax        = _rectTransform.anchorMax;
            _defaultPivot            = _rectTransform.pivot;
            _defaultAnchoredPosition = _rectTransform.anchoredPosition;
            _defaultWindowWidth      = _rectTransform.sizeDelta.x;
        }

        /// <summary>
        /// prefabに設定済みの既定位置へ戻します。
        /// </summary>
        public void SetPositionDefault()
        {
            _rectTransform.anchorMin        = _defaultAnchorMin;
            _rectTransform.anchorMax        = _defaultAnchorMax;
            _rectTransform.pivot            = _defaultPivot;
            _rectTransform.anchoredPosition = _defaultAnchoredPosition;

            _currentWindowWidth      = _defaultWindowWidth;
            _currentMessageTextWidth = DefaultMessageTextWidth;
        }

        /// <summary>
        /// 画面右上、ヘッダー直下へ配置します(雇用/解雇画面突入時の店主の会話用)。
        /// </summary>
        public void SetPositionTopRight()
        {
            _rectTransform.anchorMin        = TopRightAnchor;
            _rectTransform.anchorMax        = TopRightAnchor;
            _rectTransform.pivot            = TopRightAnchor;
            _rectTransform.anchoredPosition = TopRightAnchoredPosition;

            _currentWindowWidth      = TopRightWindowWidth;
            _currentMessageTextWidth = TopRightMessageTextWidth;
        }

        /// <summary>
        /// 話者名・セリフ・ポートレートを設定して表示します。
        /// portraitがnullの場合はポートレート画像を非表示にします。
        /// </summary>
        public void Show( string speakerName, string message, Sprite portrait )
        {
            _speakerNameText.text = speakerName;
            _messageText.text     = message;

            _portraitImage.enabled = ( portrait != null );
            _portraitImage.sprite  = portrait;

            ApplyAutoSize();

            gameObject.SetActive( true );
        }

        public void Hide()
        {
            gameObject.SetActive( false );
        }

        /// <summary>
        /// Yes/No選択肢等、セリフ欄の下に追加で確保したい高さ。派生クラスで必要な分だけ上書きします。
        /// </summary>
        protected virtual float ReservedExtraHeight => 0f;

        /// <summary>
        /// 現在の本文量に応じて、セリフ欄とウィンドウ全体の高さを再計算します。
        /// 幅は配置(既定/右上)ごとに固定したまま高さのみ本文量に合わせて可変にすることで、
        /// ローカライズによる文字列長の変化で文字がはみ出たり不自然な位置で改行されたりする問題を避けます。
        /// </summary>
        private void ApplyAutoSize()
        {
            float preferredTextHeight = _messageText.GetPreferredValues( _messageText.text, _currentMessageTextWidth, 0f ).y;

            _messageTextRect.sizeDelta = new Vector2( _currentMessageTextWidth, preferredTextHeight );

            float windowHeight = Mathf.Clamp( MessageTextTopOffset + preferredTextHeight + MessageTextBottomMargin, MinWindowHeight, MaxWindowHeight );
            windowHeight += ReservedExtraHeight;

            _rectTransform.sizeDelta = new Vector2( _currentWindowWidth, windowHeight );
        }
    }
}
