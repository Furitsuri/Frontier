using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using static Constants;

namespace Frontier.Field
{
    /// <summary>
    /// フィールド上でOPT2入力により画面左に表示するメニューリスト。
    /// 移動していない(ノードに静止している)場合に限り開くことができる。
    /// 戦闘シーンのPlCommandWindow/TileMenuWindowとは独立させ、専用のCanvasを実行時に構築して表示する。
    /// 文字列は LocalizationService を経由するため、言語切替に追従する。
    /// </summary>
    public class FieldMenuUI : MonoBehaviour
    {
        // FIELD_MENU_OPTION_TAG の並び順と対応させること
        private static readonly string[] OptionTextKeys =
        {
            "UI_CMD_STATUS",     // STATUS
            "UI_CMD_OPTION",     // OPTION
            "UI_CMD_SAVE",       // SAVE
            "UI_CMD_EXIT_GAME",  // EXIT_GAME
        };

        private const string FontResourcePath = "Fonts & Materials/Electronic Highway Sign SDF";

        [SerializeField] private Color _normalColor   = Color.white;
        [SerializeField] private Color _selectedColor = Color.red;

        [Inject] private ILocalizationService _localization = null;

        private FieldPlayerCharacterView _playerView = null;
        private CommandList _commandList = new CommandList();
        private CommandList.CommandIndexedValue _cmdIdxVal;
        private List<TextMeshProUGUI> _optionTexts = new List<TextMeshProUGUI>();
        private GameObject _panel;
        private int _baseHashCode;
        private int _menuHashCode;

        /// <summary>メニューが開いているかどうか。</summary>
        public bool IsOpen { get; private set; }

        /// <summary>
        /// メニューUIを構築し、OPT2入力の受付を開始します。
        /// </summary>
        /// <param name="playerView">移動中かどうかの判定に用いる、自身を表す3Dモデルのビュー</param>
        public void Setup( FieldPlayerCharacterView playerView )
        {
            _playerView   = playerView;
            _baseHashCode = Hash.GetStableHash( nameof( FieldMenuUI ) + "_Base" );
            _menuHashCode = Hash.GetStableHash( nameof( FieldMenuUI ) + "_Menu" );

            BuildUI();
            _panel.SetActive( false );

            if ( _localization != null ) { _localization.OnLanguageChanged += RefreshAllTexts; }

            InputFacade.Instance.RegisterInputCodes(
                ( GuideIcon.OPT2, "MENU", CanOpenMenu, new AcceptContextInput( AcceptOpen ), 0.0f, _baseHashCode )
            );
        }

        private void OnDestroy()
        {
            if ( _localization != null ) { _localization.OnLanguageChanged -= RefreshAllTexts; }
        }

        /// <summary>
        /// メニューを開けるかどうかを判定します。移動中、及び既にメニューが開いている場合は開けません。
        /// </summary>
        private bool CanOpenMenu()
        {
            return !IsOpen && _playerView != null && !_playerView.IsMoving;
        }

        private bool AcceptOpen( InputContext context )
        {
            if ( !context.GetButton( GameButton.Opt2 ) ) return false;

            OpenMenu();

            return true;
        }

        private void OpenMenu()
        {
            IsOpen = true;
            _panel.SetActive( true );

            var indices = new List<int>();
            for ( int i = 0; i < ( int ) FIELD_MENU_OPTION_TAG.NUM; ++i ) { indices.Add( i ); }

            _cmdIdxVal = new CommandList.CommandIndexedValue( 0, 0 );
            _commandList.Init( ref indices, CommandList.CommandDirection.VERTICAL, false, _cmdIdxVal );
            RefreshCursorColor();

            InputFacade.Instance.RegisterInputCodes(
                ( GuideIcon.VERTICAL_CURSOR, "SELECT",  InputFacade.CanBeAcceptAlways, new AcceptContextInput( AcceptDirection ), MENU_DIRECTION_INPUT_INTERVAL, _menuHashCode ),
                ( GuideIcon.CONFIRM,         "CONFIRM", InputFacade.CanBeAcceptAlways, new AcceptContextInput( AcceptConfirm ),   0.0f, _menuHashCode ),
                ( GuideIcon.CANCEL,          "BACK",    InputFacade.CanBeAcceptAlways, new AcceptContextInput( AcceptCancel ),    0.0f, _menuHashCode )
            );
        }

        private void CloseMenu()
        {
            IsOpen = false;
            _panel.SetActive( false );

            InputFacade.Instance.UnregisterInputCodes( _menuHashCode );

            // UnregisterInputCodesだけでは入力ガイド表示が更新されないため、空登録でガイドの再描画を促す
            InputFacade.Instance.RegisterInputCodes();
        }

        private bool AcceptDirection( InputContext context )
        {
            if ( !_commandList.OperateListCursor( context.Cursor ) ) return false;

            RefreshCursorColor();

            return true;
        }

        private bool AcceptConfirm( InputContext context )
        {
            if ( !context.GetButton( GameButton.Confirm ) ) return false;

            // MEMO: 各項目からの遷移処理は未実装。リストへの項目挿入のみ先行対応する
            var option = ( FIELD_MENU_OPTION_TAG ) _cmdIdxVal.value;
            Debug.Log( $"[FieldMenuUI] {option} は未実装です。" );

            return true;
        }

        private bool AcceptCancel( InputContext context )
        {
            if ( !context.GetButton( GameButton.Cancel ) ) return false;

            CloseMenu();

            return true;
        }

        private void RefreshCursorColor()
        {
            for ( int i = 0; i < _optionTexts.Count; ++i )
            {
                _optionTexts[i].color = ( i == _cmdIdxVal.index ) ? _selectedColor : _normalColor;
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
