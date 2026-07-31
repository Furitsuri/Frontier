using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Frontier.Field
{
    /// <summary>
    /// フィールドメニュー(OPT2)の見た目のみを担当する最小限のビュー。
    /// 開閉・カーソル位置・確定処理などの状態管理は FieldMenuPresenter が行い、
    /// このクラスは表示指示(Show/Hide/SetSelectedIndex)を受けて反映するだけに留める。
    /// 戦闘シーンのPlCommandWindow/TileMenuWindowとは独立させ、専用のCanvasを実行時に構築して表示する。
    /// </summary>
    public class FieldMenuUI : MonoBehaviour
    {
        // FIELD_MENU_OPTION_TAG の並び順と対応させること
        private static readonly string[] OptionTextKeys =
        {
            "UI_CMD_TROOPS",     // TROOPS
            "UI_CMD_OPTION",     // OPTION
            "UI_CMD_SAVE",       // SAVE
            "UI_CMD_EXIT_GAME",  // EXIT_GAME
        };

        private const string FontResourcePath = "Fonts & Materials/Electronic Highway Sign SDF";

        [SerializeField] private Color _normalColor   = Color.white;
        [SerializeField] private Color _selectedColor = Color.red;

        [Inject] private ILocalizationService _localization = null;

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

        /// <summary>選択中の項目インデックスを表示(色)に反映します。</summary>
        public void SetSelectedIndex( int index )
        {
            for ( int i = 0; i < _optionTexts.Count; ++i )
            {
                _optionTexts[i].color = ( i == index ) ? _selectedColor : _normalColor;
            }
        }

        /// <summary>
        /// 言語切替時に、表示中の全項目のテキストを現在の言語で再取得します。
        /// </summary>
        private void RefreshAllTexts()
        {
            for ( int i = 0; i < _optionTexts.Count; ++i )
            {
                _optionTexts[i].text = _localization.Get( OptionTextKeys[i] );
            }
        }

        /// <summary>
        /// 画面左寄せの縦一列メニューUIを実行時に構築します。専用のCanvasを新規生成するため、
        /// フィールドシーン側の既存Canvas設定に依存しません。
        /// </summary>
        private void BuildUI()
        {
            var canvasGO = new GameObject( "FieldMenuCanvas", typeof( RectTransform ) );
            canvasGO.transform.SetParent( transform, false );

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2( 800, 600 );

            canvasGO.AddComponent<GraphicRaycaster>();

            _panel = new GameObject( "FieldMenuPanel", typeof( RectTransform ) );
            _panel.transform.SetParent( canvasGO.transform, false );

            var panelRect = _panel.GetComponent<RectTransform>();
            panelRect.anchorMin        = new Vector2( 0f, 0.5f );
            panelRect.anchorMax        = new Vector2( 0f, 0.5f );
            panelRect.pivot            = new Vector2( 0f, 0.5f );
            panelRect.anchoredPosition = new Vector2( 24f, 0f );

            var bgImage = _panel.AddComponent<Image>();
            bgImage.color = new Color( 0f, 0f, 0f, 0.6f );

            var layout = _panel.AddComponent<VerticalLayoutGroup>();
            layout.padding                = new RectOffset( 16, 16, 12, 12 );
            layout.spacing                = 8f;
            layout.childAlignment         = TextAnchor.MiddleLeft;
            layout.childControlWidth      = true;
            layout.childControlHeight     = true;
            layout.childForceExpandWidth  = false;
            layout.childForceExpandHeight = false;

            var fitter = _panel.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

            var font = Resources.Load<TMP_FontAsset>( FontResourcePath );

            for ( int i = 0; i < OptionTextKeys.Length; ++i )
            {
                var itemGO = new GameObject( "Item_" + OptionTextKeys[i], typeof( RectTransform ) );
                itemGO.transform.SetParent( _panel.transform, false );

                var text = itemGO.AddComponent<TextMeshProUGUI>();
                if ( font != null ) { text.font = font; }
                text.fontSize  = 24;
                text.text      = _localization != null ? _localization.Get( OptionTextKeys[i] ) : OptionTextKeys[i];
                text.color     = _normalColor;
                text.alignment = TextAlignmentOptions.MidlineLeft;
                text.enableWordWrapping = false;
                text.overflowMode = TextOverflowModes.Overflow;

                var le = itemGO.AddComponent<LayoutElement>();
                le.minWidth  = 160f;
                le.minHeight = 36f;

                _optionTexts.Add( text );
            }
        }
    }
}
