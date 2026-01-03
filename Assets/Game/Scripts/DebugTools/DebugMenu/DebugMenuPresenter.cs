using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TMPro;
using UnityEngine;
using Zenject;

namespace Frontier.DebugTools.DebugMenu
{
    public class DebugMenuPresenter
    {
        [Inject] private IUiSystem _uiSystem                = null;

        private DebugMenuUI _debugMenuUI;

        public void Init()
        {
            _debugMenuUI = _uiSystem.DebugUi.DebugMenuView;
            _debugMenuUI.Setup();
        }

        public void UpdateMenuCursor( int index )
        {
            // 選択中のメニューのテキストを強調表示
            for( int i = 0; i < _debugMenuUI.MenuTexts.Count; ++i )
            {
                if( i == index )
                {
                    _debugMenuUI.MenuTexts[i].color = Color.yellow;
                }
                else
                {
                    _debugMenuUI.MenuTexts[i].color = Color.white;
                }
            }
        }

        public ReadOnlyCollection<TextMeshProUGUI> MenuTexts()
        {
            return _debugMenuUI.MenuTexts;
        }

        public void ToggleMenuVisibility()
        {
            _debugMenuUI.gameObject.SetActive( !_debugMenuUI.gameObject.activeSelf );
        }
    }
}