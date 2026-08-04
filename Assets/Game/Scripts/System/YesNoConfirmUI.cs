using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Frontier
{
    /// <summary>
    /// 二者択一(YES/NO)の確認ダイアログの見た目のみを担当する最小限のビュー。
    /// 開閉・カーソル位置の管理は YesNoConfirmPresenter が行い、このクラスは表示指示を受けて反映するだけに留める。
    /// メッセージはSetMessage()でローカライズキーを渡すことで、ゲーム終了確認・セーブデータ削除確認等、
    /// 複数の用途に使い回せる。
    /// </summary>
    public class YesNoConfirmUI : MonoBehaviour
    {
        private static readonly string[] OptionTextKeys =
        {
            "UI_CONFIRM_YES",
            "UI_CONFIRM_NO",
        };

        private const string FontResourcePath = "Fonts & Materials/Electronic Highway Sign SDF";

        [SerializeField] private Color _normalColor   = Color.white;
        [SerializeField] private Color _selectedColor = Color.red;

        [Inject] private ILocalizationService _localization = null;

        private string _messageKey = "";
        private TextMeshProUGUI _messageText = null;
        private TextMeshProUGUI _detailText = null;
        private List<TextMeshProUGUI> _optionTexts = new List<TextMeshProUGUI>();
        private GameObject _panel;

        /// <summary>UIを構築します。初期状態は非表示です。</summary>
        public void Setup()
        {
            BuildUI();
            Hide();

            if ( _localization != null ) { _localization.OnLanguageChanged += RefreshAllTexts; }
        }

        private void OnDestroy()
        {
            if ( _localization != null ) { _localization.OnLanguageChanged -= RefreshAllTexts; }
        }

        public void Show() => _panel.SetActive( true );

        public void Hide() => _panel.SetActive( false );

        /// <summary>
        /// 確認メッセージを設定します。ローカライズキーを渡してください
        /// (例: "UI_CONFIRM_EXIT_GAME_MESSAGE" / "UI_CONFIRM_DELETE_SAVE_MESSAGE")。
        /// </summary>
        public void SetMessage( string messageKey )
        {
            _messageKey = messageKey;
            RefreshAllTexts();
        }

        /// <summary>
        /// メッセージの下に表示する詳細テキスト(例: 所持金の増減 "1000 → 900")を設定します。
        /// nullまたは空文字列を渡した場合は非表示にします(既定では非表示のため、使用しない呼び出し元には影響しません)。
        /// </summary>
        public void SetDetailText( string text )
        {
            if ( _detailText == null ) return;

            bool show = !string.IsNullOrEmpty( text );
            _detailText.gameObject.SetActive( show );
            if ( show ) { _detailText.text = text; }
        }

        /// <summary>選択中の項目インデックス(0:YES, 1:NO)を表示(色)に反映します。</summary>
        public void SetSelectedIndex( int index )
        {
            for ( int i = 0; i < _optionTexts.Count; ++i )
            {
                _optionTexts[i].color = ( i == index ) ? _selectedColor : _normalColor;
            }
        }

        /// <summary>
        /// 言語切替時に、表示中のメッセージ・項目のテキストを現在の言語で再取得します。
        /// </summary>
        private void RefreshAllTexts()
        {
            if ( _messageText != null )
            {
                _messageText.text = ( _localization != null && !string.IsNullOrEmpty( _messageKey ) ) ? _localization.Get( _messageKey ) : _messageKey;
            }

            for ( int i = 0; i < _optionTexts.Count; ++i )
            {
                _optionTexts[i].text = _localization != null ? _localization.Get( OptionTextKeys[i] ) : OptionTextKeys[i];
            }
        }

        /// <summary>
        /// 画面中央に確認メッセージとYES/NOの横並びメニューを実行時に構築します。
        /// 専用のCanvasを新規生成するため、呼び出し元シーン側の既存Canvas設定に依存しません。
        /// </summary>
        private void BuildUI()
        {
            var canvasGO = new GameObject( "YesNoConfirmCanvas", typeof( RectTransform ) );
            canvasGO.transform.SetParent( transform, false );

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 210; // メニュー(100〜200)より手前に表示する

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2( 800, 600 );

            canvasGO.AddComponent<GraphicRaycaster>();

            _panel = new GameObject( "YesNoConfirmPanel", typeof( RectTransform ) );
            _panel.transform.SetParent( canvasGO.transform, false );

            var panelRect = _panel.GetComponent<RectTransform>();
            panelRect.anchorMin        = new Vector2( 0.5f, 0.5f );
            panelRect.anchorMax        = new Vector2( 0.5f, 0.5f );
            panelRect.pivot            = new Vector2( 0.5f, 0.5f );
            panelRect.anchoredPosition = Vector2.zero;

            var bgImage = _panel.AddComponent<Image>();
            bgImage.color = new Color( 0f, 0f, 0f, 0.8f );

            var panelLayout = _panel.AddComponent<VerticalLayoutGroup>();
            panelLayout.padding                = new RectOffset( 32, 32, 24, 24 );
            panelLayout.spacing                = 20f;
            panelLayout.childAlignment         = TextAnchor.MiddleCenter;
            panelLayout.childControlWidth      = true;
            panelLayout.childControlHeight     = true;
            panelLayout.childForceExpandWidth  = false;
            panelLayout.childForceExpandHeight = false;

            var panelFitter = _panel.AddComponent<ContentSizeFitter>();
            panelFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            panelFitter.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

            var font = Resources.Load<TMP_FontAsset>( FontResourcePath );

            // メッセージ
            var messageGO = new GameObject( "Message", typeof( RectTransform ) );
            messageGO.transform.SetParent( _panel.transform, false );

            _messageText = messageGO.AddComponent<TextMeshProUGUI>();
            if ( font != null ) { _messageText.font = font; }
            _messageText.fontSize   = 24;
            _messageText.text       = "";
            _messageText.color      = _normalColor;
            _messageText.alignment  = TextAlignmentOptions.Center;
            _messageText.enableWordWrapping = false;
            _messageText.overflowMode       = TextOverflowModes.Overflow;

            var messageLe = messageGO.AddComponent<LayoutElement>();
            messageLe.minWidth  = 320f;
            messageLe.minHeight = 36f;

            // 詳細テキスト(例: 所持金の増減表示)。既定では非表示で、使用する場合はSetDetailText()で表示する
            var detailGO = new GameObject( "Detail", typeof( RectTransform ) );
            detailGO.transform.SetParent( _panel.transform, false );

            _detailText = detailGO.AddComponent<TextMeshProUGUI>();
            if ( font != null ) { _detailText.font = font; }
            _detailText.fontSize   = 24;
            _detailText.text       = "";
            _detailText.color      = _normalColor;
            _detailText.alignment  = TextAlignmentOptions.Center;
            _detailText.enableWordWrapping = false;
            _detailText.overflowMode       = TextOverflowModes.Overflow;

            var detailLe = detailGO.AddComponent<LayoutElement>();
            detailLe.minWidth  = 320f;
            detailLe.minHeight = 36f;

            detailGO.SetActive( false );

            // YES/NOの横並び行
            var rowGO = new GameObject( "Options", typeof( RectTransform ) );
            rowGO.transform.SetParent( _panel.transform, false );

            var rowLayout = rowGO.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing                = 48f;
            rowLayout.childAlignment         = TextAnchor.MiddleCenter;
            rowLayout.childControlWidth      = true;
            rowLayout.childControlHeight     = true;
            rowLayout.childForceExpandWidth  = false;
            rowLayout.childForceExpandHeight = false;

            var rowFitter = rowGO.AddComponent<ContentSizeFitter>();
            rowFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            rowFitter.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

            for ( int i = 0; i < OptionTextKeys.Length; ++i )
            {
                var itemGO = new GameObject( "Item_" + OptionTextKeys[i], typeof( RectTransform ) );
                itemGO.transform.SetParent( rowGO.transform, false );

                var text = itemGO.AddComponent<TextMeshProUGUI>();
                if ( font != null ) { text.font = font; }
                text.fontSize  = 24;
                text.text      = _localization != null ? _localization.Get( OptionTextKeys[i] ) : OptionTextKeys[i];
                text.color     = _normalColor;
                text.alignment = TextAlignmentOptions.Center;
                text.enableWordWrapping = false;
                text.overflowMode = TextOverflowModes.Overflow;

                var le = itemGO.AddComponent<LayoutElement>();
                le.minWidth  = 100f;
                le.minHeight = 36f;

                _optionTexts.Add( text );
            }
        }
    }
}
