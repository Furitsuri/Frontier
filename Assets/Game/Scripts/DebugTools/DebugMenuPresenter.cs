using Frontier;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class DebugMenuPresenter : MonoBehaviour
{
    private HierarchyBuilderBase _hierarchyBld = null;

    [SerializeField]
    private GameObject DebugMenuList;
    [SerializeField]
    private GameObject DebugMenuElement;

    private List<TextMeshProUGUI> _menuTexts = new List<TextMeshProUGUI>();
    private VerticalLayoutGroup _verticalLayoutGroup;

    public ReadOnlyCollection<TextMeshProUGUI> MenuTexts
    {
        get { return _menuTexts.AsReadOnly(); }
    }

    [Inject]
    public void Construct(HierarchyBuilderBase hierarchyBld)
    {
        _hierarchyBld = hierarchyBld;
    }

    /// <summary>
    /// 初期化します
    /// </summary>
    public void Init()
    {   
        _verticalLayoutGroup = DebugMenuList.GetComponent<VerticalLayoutGroup>();
        NullCheck.AssertNotNull(_verticalLayoutGroup);

        InitDebugMenuList();

        gameObject.SetActive(false);    // 初期状態では無効にする
    }

    /// <summary>
    /// メニューカーソルを更新します
    /// </summary>
    public void UpdateMenuCursor( int index )
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

    /// <summary>
    /// デバッグメニューの表示/非表示を切り替えます
    /// </summary>
    public void ToggleMenuVisibility()
    {         
        gameObject.SetActive(!gameObject.activeSelf);
    }

    /// <summary>
    /// デバッグのメニューリストを初期化します
    /// </summary>
    private void InitDebugMenuList()
    {
        _menuTexts.Clear();

        for (int i = 0; i < (int)DebugMainMenu.MAX; ++i)
        {
            TextMeshProUGUI textUGUI = _hierarchyBld.CreateComponentWithNestedParent<TextMeshProUGUI>(DebugMenuElement, DebugMenuList, true );
            textUGUI.text = ((DebugMainMenu)i).ToString().Replace('_', ' ');    // アンダースコアをスペースに置き換え
            _menuTexts.Add(textUGUI);
        }
    }
}