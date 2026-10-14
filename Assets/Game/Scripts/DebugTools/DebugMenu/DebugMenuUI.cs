using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Zenject;

#if UNITY_EDITOR

namespace Frontier.DebugTools.DebugMenu
{
    public class DebugMenuUI : UiMonoBehaviour
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;

        [SerializeField] private GameObject DebugMenuList;
        [SerializeField] private GameObject DebugMenuElement;
        [SerializeField] private TextMeshProUGUI _headline;     // ページの見出し

        private static readonly Color ColorSelected   = Color.yellow;
        private static readonly Color ColorUnselected = Color.white;

        private List<TextMeshProUGUI> _rowTexts = new List<TextMeshProUGUI>();

        public override void Setup()
        {
            gameObject.SetActive( false );    // 初期状態では無効にする
        }

        /// <summary>
        /// 見出しの文字列を設定します
        /// </summary>
        public void SetHeadline( string text )
        {
            if( _headline != null ) { _headline.text = text; }
        }

        /// <summary>
        /// 各行の文字列を設定します。行数が足りない場合は行を追加し、余った行は非表示にします
        /// </summary>
        public void SetRows( IReadOnlyList<string> texts )
        {
            while( _rowTexts.Count < texts.Count )
            {
                _rowTexts.Add( _hierarchyBld.CreateComponentWithNestedParent<TextMeshProUGUI>( DebugMenuElement, DebugMenuList, true ) );
            }

            for( int i = 0; i < _rowTexts.Count; ++i )
            {
                bool isUsed = ( i < texts.Count );
                if( _rowTexts[i].gameObject.activeSelf != isUsed ) { _rowTexts[i].gameObject.SetActive( isUsed ); }
                if( isUsed ) { _rowTexts[i].text = texts[i]; }
            }
        }

        /// <summary>
        /// 指定の行を選択中として強調表示します
        /// </summary>
        public void SetSelectedRow( int index )
        {
            for( int i = 0; i < _rowTexts.Count; ++i )
            {
                _rowTexts[i].color = ( i == index ) ? ColorSelected : ColorUnselected;
            }
        }
    }
}

#endif // UNITY_EDITOR
