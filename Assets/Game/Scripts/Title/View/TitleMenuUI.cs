using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Frontier.Title
{
    /// <summary>
    /// タイトルメニューの見た目のみを担当する最小限のビュー。
    /// カーソル位置・確定処理などの状態管理はTitleMenuPresenterが行い、
    /// このクラスは表示指示(SetSelectedIndex/SetLoadGameVisible)を受けて反映するだけに留める。
    /// FieldMenuUIと同様、専用のCanvasを実行時に構築して表示する。
    /// </summary>
    public class TitleMenuUI : MonoBehaviour
    {
        // TITLE_MENU_OPTION_TAG の並び順と対応させること
        private static readonly LocKey[] OptionTextKeys =
        {
            LocKey.UI_CMD_NEW_GAME,   // NEW_GAME
            LocKey.UI_CMD_LOAD_GAME,  // LOAD_GAME
            LocKey.UI_CMD_OPTION,     // OPTION
            LocKey.UI_CMD_EXIT_GAME,  // EXIT_GAME
        };

        private const string FontResourcePath = "Fonts & Materials/Electronic Highway Sign SDF";

        [SerializeField] private Color _normalColor   = Color.white;
        [SerializeField] private Color _selectedColor = Color.red;

        [Inject] private ILocalizationService _localization = null;

        private List<TextMeshProUGUI> _optionTexts = new List<TextMeshProUGUI>();

        /// <summary>UIを構築します。</summary>
        public void Setup()
        {
            BuildUI();

            if ( _localization != null ) { _localization.OnLanguageChanged += RefreshAllTexts; }
        }

        private void OnDestroy()
        {
            if ( _localization != null ) { _localization.OnLanguageChanged -= RefreshAllTexts; }
        }

        /// <summary>選択中の項目インデックスを表示(色)に反映します。</summary>
        public void SetSelectedIndex( int index )
        {
            for ( int i = 0; i < _optionTexts.Count; ++i )
            {
                _optionTexts[i].color = ( i == index ) ? _selectedColor : _normalColor;
            }
        }

        /// <summary>
        /// LOAD GAME項目の表示/非表示を切り替えます。セーブデータが存在しない場合は非表示にします。
        /// </summary>
        public void SetLoadGameVisible( bool visible )
        {
            _optionTexts[( int ) TITLE_MENU_OPTION_TAG.LOAD_GAME].gameObject.SetActive( visible );
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
        /// 画面中央寄せの縦一列メニューUIを実行時に構築します。専用のCanvasを新規生成するため、
        /// タイトルシーン側の既存Canvas設定に依存しません。
        /// </summary>
        private void BuildUI()
        {
            var canvasGO = new GameObject( "TitleMenuCanvas", typeof( RectTransform ) );
            canvasGO.transform.SetParent( transform, false );

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            // タイトルシーン内の他のCanvas(TitleUI: sortingOrder=5)より確実に手前に描画するため、高めの値にする
            canvas.sortingOrder = 200;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2( 800, 600 );

            canvasGO.AddComponent<GraphicRaycaster>();

            var panel = new GameObject( "TitleMenuPanel", typeof( RectTransform ) );
            panel.transform.SetParent( canvasGO.transform, false );

            var panelRect = panel.GetComponent<RectTransform>();
            // ロゴ・タイトルテキスト(画面上部)と下部の入力ガイドバーの間、中央よりやや下寄りに配置する
            panelRect.anchorMin        = new Vector2( 0.5f, 0.4f );
            panelRect.anchorMax        = new Vector2( 0.5f, 0.4f );
            panelRect.pivot            = new Vector2( 0.5f, 0.5f );
            panelRect.anchoredPosition = Vector2.zero;

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding                = new RectOffset( 16, 16, 4, 4 );
            layout.spacing                = 3f;
            layout.childAlignment         = TextAnchor.MiddleCenter;
            layout.childControlWidth      = true;
            layout.childControlHeight     = true;
            layout.childForceExpandWidth  = false;
            layout.childForceExpandHeight = false;

            var fitter = panel.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

            var font = Resources.Load<TMP_FontAsset>( FontResourcePath );

            for ( int i = 0; i < OptionTextKeys.Length; ++i )
            {
                var itemGO = new GameObject( "Item_" + OptionTextKeys[i], typeof( RectTransform ) );
                itemGO.transform.SetParent( panel.transform, false );

                var text = itemGO.AddComponent<TextMeshProUGUI>();
                if ( font != null ) { text.font = font; }
                text.fontSize  = 15;
                text.text      = _localization != null ? _localization.Get( OptionTextKeys[i] ) : OptionTextKeys[i].ToString();
                text.color     = _normalColor;
                text.alignment = TextAlignmentOptions.Midline;
                text.enableWordWrapping = false;
                text.overflowMode = TextOverflowModes.Overflow;

                var le = itemGO.AddComponent<LayoutElement>();
                le.minWidth  = 220f;
                le.minHeight = 18f;

                _optionTexts.Add( text );
            }
        }
    }
}
