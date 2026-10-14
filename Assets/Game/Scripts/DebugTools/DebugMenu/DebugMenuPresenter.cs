using System.Collections.Generic;
using Zenject;

#if UNITY_EDITOR

namespace Frontier.DebugTools.DebugMenu
{
    public class DebugMenuPresenter
    {
        [Inject] private IUiSystem _uiSystem = null;

        private const string CATEGORY_LIST_HEADLINE = "DEBUG MENU";
        private const string NOT_IMPLEMENTED_TEXT   = "NOT IMPLEMENTED";    // ページが1つも無いカテゴリを開いた際の表記
        private const string HEADLINE_SEPARATOR     = " > ";
        private const float VALUE_COLUMN_GAP        = 24f;  // 最も長い項目名の右端から、値の列までの間隔
        private const int NO_SELECTION              = -1;

        // 項目名と値の間に挟む区切り。項目の型ごとに一律で適用する(登録されていない型は区切り無し。ACTIONは表示する値を持たないため対象外)
        private static readonly Dictionary<DebugMenuItemType, string> ValueSeparators = new Dictionary<DebugMenuItemType, string>
        {
            { DebugMenuItemType.TOGGLE,    ": " },
            { DebugMenuItemType.INT,       ": " },
            { DebugMenuItemType.CHOICE,    ": " },
            { DebugMenuItemType.READ_ONLY, ": " },
        };

        private DebugMenuUI _debugMenuUI;
        private List<string> _rowTexts = new List<string>();

        public void Init()
        {
            _debugMenuUI = _uiSystem.DebugUi.DebugMenuView;
            _debugMenuUI.Setup();
        }

        /// <summary>
        /// カテゴリの一覧を表示します
        /// </summary>
        public void ShowCategoryList( IReadOnlyList<DebugMenuCategory> categories, int selectedIndex )
        {
            _rowTexts.Clear();
            foreach( var category in categories )
            {
                _rowTexts.Add( ToDisplayName( category ) );
            }

            Show( CATEGORY_LIST_HEADLINE, selectedIndex );
        }

        /// <summary>
        /// 指定カテゴリのページの一覧を表示します。ページが1つも無い場合は未実装である旨を表示します
        /// </summary>
        public void ShowPageList( DebugMenuCategory category, IReadOnlyList<DebugMenuPage> pages, int selectedIndex )
        {
            _rowTexts.Clear();
            foreach( var page in pages )
            {
                _rowTexts.Add( page.Name );
            }

            if( pages.Count <= 0 )
            {
                _rowTexts.Add( NOT_IMPLEMENTED_TEXT );
                selectedIndex = NO_SELECTION;
            }

            Show( ToDisplayName( category ), selectedIndex );
        }

        /// <summary>
        /// 指定ページの項目の一覧を、各項目の現在値と共に表示します。
        /// 値(区切りを含む)は、ページ内で最も長い項目名の右側に揃えた列に表示します
        /// </summary>
        public void ShowItemPage( DebugMenuPage page, IReadOnlyList<DebugMenuItem> items, int selectedIndex )
        {
            string valueColumnTag = $"<pos={CalcValueColumnPosition( items ):0}>";

            _rowTexts.Clear();
            foreach( var item in items )
            {
                ValueSeparators.TryGetValue( item.Type, out var separator );
                _rowTexts.Add( item.Name + valueColumnTag + separator + item.GetValueText() );
            }

            Show( ToDisplayName( page.Category ) + HEADLINE_SEPARATOR + page.Name, selectedIndex );
        }

        public void ToggleMenuVisibility()
        {
            _debugMenuUI.gameObject.SetActive( !_debugMenuUI.gameObject.activeSelf );
        }

        private void Show( string headline, int selectedIndex )
        {
            _debugMenuUI.SetHeadline( headline );
            _debugMenuUI.SetRows( _rowTexts );
            _debugMenuUI.SetSelectedRow( selectedIndex );
        }

        /// <summary>
        /// 値の列を表示する位置(行の左端からの距離)を、ページ内で最も長い項目名の幅から求めます
        /// </summary>
        private float CalcValueColumnPosition( IReadOnlyList<DebugMenuItem> items )
        {
            float maxNameWidth = 0f;
            foreach( var item in items )
            {
                maxNameWidth = System.Math.Max( maxNameWidth, _debugMenuUI.MeasureRowTextWidth( item.Name ) );
            }

            return maxNameWidth + VALUE_COLUMN_GAP;
        }

        private static string ToDisplayName( DebugMenuCategory category )
        {
            return category.ToString().Replace( '_', ' ' );    // アンダースコアをスペースに置き換え
        }
    }
}

#endif // UNITY_EDITOR
