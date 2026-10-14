using System;
using System.Collections.Generic;
using System.Text;

#if UNITY_EDITOR

namespace Frontier.DebugTools.DebugMenu
{
    /// <summary>
    /// デバッグメニューの項目の登録簿です。
    /// 項目は登録元(owner)単位で登録・解除します(戦闘終了時に戦闘関連の項目をまとめて外す、など)。
    /// メニューの表示だけでなく、コードから項目を名前で引いて操作したり、全項目の現在値をテキストで出力する窓口も兼ねます
    /// </summary>
    public class DebugMenuItemRegistry
    {
        private readonly List<(object owner, DebugMenuItem item)> _items = new List<(object, DebugMenuItem)>();
        private readonly List<(object owner, DebugMenuPage page, Action callback)> _pageOpenedCallbacks = new List<(object, DebugMenuPage, Action)>();

        /// <summary>
        /// 項目を登録します。同じページ内では登録した順に表示され、同じカテゴリ内のページは最初に項目が登録された順に表示されます
        /// </summary>
        /// <param name="owner">登録元。UnregisterAllでまとめて解除する際のキーになります</param>
        /// <param name="item">登録する項目</param>
        public void Register( object owner, DebugMenuItem item )
        {
            _items.Add( (owner, item) );
        }

        /// <summary>
        /// 指定のページが開かれた際に呼ばれるコールバックを登録します(操作対象の選び直しなど)
        /// </summary>
        public void RegisterPageOpenedCallback( object owner, DebugMenuPage page, Action callback )
        {
            _pageOpenedCallbacks.Add( (owner, page, callback) );
        }

        /// <summary>
        /// 指定の登録元が登録した項目・コールバックを全て解除します
        /// </summary>
        public void UnregisterAll( object owner )
        {
            _items.RemoveAll( entry => entry.owner == owner );
            _pageOpenedCallbacks.RemoveAll( entry => entry.owner == owner );
        }

        /// <summary>
        /// 指定カテゴリの、項目が1つ以上登録されているページを取得します
        /// </summary>
        public List<DebugMenuPage> GetPages( DebugMenuCategory category )
        {
            var pages = new List<DebugMenuPage>();

            foreach( var entry in _items )
            {
                var page = entry.item.Page;
                if( page.Category == category && !pages.Contains( page ) ) { pages.Add( page ); }
            }

            return pages;
        }

        /// <summary>
        /// 指定ページの項目を登録順で取得します
        /// </summary>
        public List<DebugMenuItem> GetItems( DebugMenuPage page )
        {
            var items = new List<DebugMenuItem>();

            foreach( var entry in _items )
            {
                if( entry.item.Page == page ) { items.Add( entry.item ); }
            }

            return items;
        }

        /// <summary>
        /// 指定のページが開かれたことを通知します
        /// </summary>
        public void NotifyPageOpened( DebugMenuPage page )
        {
            foreach( var entry in _pageOpenedCallbacks )
            {
                if( entry.page == page ) { entry.callback(); }
            }
        }

        /// <summary>
        /// ページと名前から項目を取得します。見つからない、または型が異なる場合はnullを返します
        /// </summary>
        public T Find<T>( DebugMenuPage page, string name ) where T : DebugMenuItem
        {
            foreach( var entry in _items )
            {
                if( entry.item.Page == page && entry.item.Name == name ) { return entry.item as T; }
            }

            return null;
        }

        /// <summary>
        /// 登録されている全項目の現在値を、カテゴリ・ページ毎にテキストで出力します
        /// </summary>
        public string DumpAsText()
        {
            var sb = new StringBuilder();

            for( int i = 0; i < ( int ) DebugMenuCategory.NUM; ++i )
            {
                var category = ( DebugMenuCategory ) i;
                sb.AppendLine( $"[{category}]" );

                foreach( var page in GetPages( category ) )
                {
                    sb.AppendLine( $"  <{page.Name}>" );

                    foreach( var item in GetItems( page ) )
                    {
                        sb.AppendLine( $"    {item.Name} ({item.Type}) : {item.GetValueText()}" );
                    }
                }
            }

            return sb.ToString();
        }
    }
}

#endif // UNITY_EDITOR
