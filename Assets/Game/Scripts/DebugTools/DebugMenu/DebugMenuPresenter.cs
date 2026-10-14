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
        private const string VALUE_COLUMN_POS       = "<pos=360>";  // 項目名に続けて値を表示する位置(行の左端からの距離)
        private const int NO_SELECTION              = -1;

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
        /// 指定ページの項目の一覧を、各項目の現在値と共に表示します
        /// </summary>
        public void ShowItemPage( DebugMenuPage page, IReadOnlyList<DebugMenuItem> items, int selectedIndex )
        {
            _rowTexts.Clear();
            foreach( var item in items )
            {
                _rowTexts.Add( item.Name + VALUE_COLUMN_POS + item.GetValueText() );
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

        private static string ToDisplayName( DebugMenuCategory category )
        {
            return category.ToString().Replace( '_', ' ' );    // アンダースコアをスペースに置き換え
        }
    }
}

#endif // UNITY_EDITOR
