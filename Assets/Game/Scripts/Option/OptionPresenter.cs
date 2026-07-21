using Frontier.Registries;
using Frontier.UI;
using System;
using System.Collections.Generic;
using Zenject;

namespace Frontier.Option
{
    /// <summary>
    /// オプション画面の表示・操作を担うPresenter。
    /// IOptionItemのリストから、種類に応じたUI項目を動的に生成してOptionUIに配置し、
    /// 値変更を各項目のApply()へ橋渡しします。
    /// </summary>
    public class OptionPresenter
    {
        [Inject] private IUiSystem _uiSystem = null;
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;
        [Inject] private PrefabRegistry _prefabReg = null;

        private OptionUI _optionUI = null;
        private List<IOptionItem> _items = null;

        public event Action OnCloseRequested;

        /// <summary>
        /// 初期化を行い、項目リストからUIを構築します
        /// </summary>
        /// <param name="items">表示する設定項目のリスト</param>
        public void Init( List<IOptionItem> items )
        {
            _optionUI = _uiSystem.GeneralUi.OptionView;
            _optionUI.Setup();
            _items = items;

            _optionUI.CloseButton.onClick.AddListener( () => OnCloseRequested?.Invoke() );

            BuildItemUIs();
        }

        /// <summary>
        /// オプション画面を表示します
        /// </summary>
        public void Show()
        {
            _optionUI.gameObject.SetActive( true );
        }

        /// <summary>
        /// オプション画面を非表示にします
        /// </summary>
        public void Hide()
        {
            _optionUI.gameObject.SetActive( false );
        }

        /// <summary>
        /// 項目リストの種類ごとに対応するUIを生成し、ContentRoot配下に並べます
        /// </summary>
        private void BuildItemUIs()
        {
            foreach( var item in _items )
            {
                switch( item )
                {
                    case SliderOptionItem sliderItem:
                        BuildSliderItemUI( sliderItem );
                        break;

                    case ToggleOptionItem toggleItem:
                        BuildToggleItemUI( toggleItem );
                        break;
                }
            }
        }

        private void BuildSliderItemUI( SliderOptionItem item )
        {
            var itemUI = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<SliderOptionItemUI>( _prefabReg.SliderOptionItemPrefab, true, false, item.Label );
            itemUI.Setup();
            itemUI.transform.SetParent( _optionUI.ContentRoot, false );
            // 個々の項目行はOption画面自体(親)の表示/非表示に追従させるため、常時アクティブにする
            itemUI.gameObject.SetActive( true );

            itemUI.SetLabel( item.Label );
            itemUI.SetRange( item.MinValue, item.MaxValue );
            itemUI.SetValueWithoutNotify( item.CurrentValue );
            itemUI.OnValueChanged += item.Apply;
        }

        private void BuildToggleItemUI( ToggleOptionItem item )
        {
            var itemUI = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<ToggleOptionItemUI>( _prefabReg.ToggleOptionItemPrefab, true, false, item.Label );
            itemUI.Setup();
            itemUI.gameObject.SetActive( true );
            itemUI.transform.SetParent( _optionUI.ContentRoot, false );

            itemUI.SetLabel( item.Label );
            itemUI.SetValueWithoutNotify( item.CurrentValue );
            itemUI.OnValueChanged += item.Apply;
        }
    }
}
