using System.Collections.Generic;
using System.Collections.ObjectModel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

#if UNITY_EDITOR

namespace Frontier.DebugTools.DebugMenu
{
    public class DebugMenuUI : UiMonoBehaviour
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;

        [SerializeField] private GameObject DebugMenuList;
        [SerializeField] private GameObject DebugMenuElement;

        private List<TextMeshProUGUI> _menuTexts = new List<TextMeshProUGUI>();
        private VerticalLayoutGroup _verticalLayoutGroup;

        public ReadOnlyCollection<TextMeshProUGUI> MenuTexts
        {
            get { return _menuTexts.AsReadOnly(); }
        }

        /// <summary>
        /// デバッグのメニューリストを初期化します
        /// </summary>
        private void InitDebugMenuList()
        {
            _menuTexts.Clear();

            for( int i = 0; i < ( int ) DebugMainMenuTag.MAX; ++i )
            {
                TextMeshProUGUI textUGUI = null;
                LazyInject.GetOrCreate( ref textUGUI, () => _hierarchyBld.CreateComponentWithNestedParent<TextMeshProUGUI>( DebugMenuElement, DebugMenuList, true ) );
                textUGUI.text = ( ( DebugMainMenuTag ) i ).ToString().Replace( '_', ' ' );    // アンダースコアをスペースに置き換え
                _menuTexts.Add( textUGUI );
            }
        }

        public override void Setup()
        {
            LazyInject.GetOrCreate( ref _verticalLayoutGroup, () => DebugMenuList.GetComponent<VerticalLayoutGroup>() );

            InitDebugMenuList();

            gameObject.SetActive( false );    // 初期状態では無効にする
        }
    }
}

#endif // UNITY_EDITOR