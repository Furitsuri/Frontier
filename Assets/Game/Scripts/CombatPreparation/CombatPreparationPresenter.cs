using Frontier.DebugTools.DebugMenu;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Frontier.CombatPreparation
{
    public class CombatPreparationPresenter : BasePresenter
    {
        [Inject] HierarchyBuilderBase _hierarchyBld = null;

        [SerializeField] private GameObject MenuList;
        [SerializeField] private GameObject MenuElement;

        private List<TextMeshProUGUI> _menuTexts = new List<TextMeshProUGUI>();

        override public void Init()
        {
            InitMenuList(_menuTexts);

            gameObject.SetActive(false);    // 初期状態では無効にする
        }

        /// <summary>
        /// メニューカーソルを更新します
        /// </summary>
        public void UpdateMenuCursor(int index)
        {
            // 選択中のメニューのテキストを強調表示
            for (int i = 0; i < _menuTexts.Count; ++i)
            {
                if (i == index)
                {
                    _menuTexts[i].color = Color.yellow;
                }
                else
                {
                    _menuTexts[i].color = Color.white;
                }
            }
        }

        private void InitMenuList(List<TextMeshProUGUI> menuTexts)
        {
            menuTexts.Clear();

            for (int i = 0; i < (int)DebugMainMenuTag.MAX; ++i)
            {
                TextMeshProUGUI textUGUI = _hierarchyBld.CreateComponentWithNestedParent<TextMeshProUGUI>(MenuElement, MenuList, true);
                textUGUI.text = ((DebugMainMenuTag)i).ToString().Replace('_', ' ');    // アンダースコアをスペースに置き換え
                menuTexts.Add(textUGUI);
            }
        }
    }
}