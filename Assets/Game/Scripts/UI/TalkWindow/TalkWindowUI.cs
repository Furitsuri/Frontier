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
        private static readonly Vector2 TopRightAnchor          = new Vector2( 1f, 1f );
        private static readonly Vector2 TopRightAnchoredPosition = new Vector2( -24f, -65f );
        private static readonly Vector2 TopRightSizeDelta        = new Vector2( 560f, 130f );

        private RectTransform _rectTransform;
        private Vector2 _defaultAnchorMin;
        private Vector2 _defaultAnchorMax;
        private Vector2 _defaultPivot;
        private Vector2 _defaultAnchoredPosition;
        private Vector2 _defaultSizeDelta;

        public override void Setup()
        {
            base.Setup();

            // prefabに設定済みの位置(雇用/解雇選択のクッション画面用)を既定値として保持しておく
            _rectTransform = ( RectTransform ) transform;
            _defaultAnchorMin        = _rectTransform.anchorMin;
            _defaultAnchorMax        = _rectTransform.anchorMax;
            _defaultPivot            = _rectTransform.pivot;
            _defaultAnchoredPosition = _rectTransform.anchoredPosition;
            _defaultSizeDelta        = _rectTransform.sizeDelta;
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
            _rectTransform.sizeDelta        = _defaultSizeDelta;
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
            _rectTransform.sizeDelta        = TopRightSizeDelta;
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

            gameObject.SetActive( true );
        }

        public void Hide()
        {
            gameObject.SetActive( false );
        }
    }
}
