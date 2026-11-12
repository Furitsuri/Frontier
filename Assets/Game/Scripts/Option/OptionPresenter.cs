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
    /// また、キーボード・パッドによるカーソル選択(項目一覧 + 閉じるボタン)を管理します。
    /// </summary>
    public class OptionPresenter
    {
        [Inject] private IUiSystem _uiSystem = null;
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;
        [Inject] private PrefabRegistry _prefabReg = null;

        private OptionUI _optionUI = null;
        private List<IOptionItem> _items = null;
        private List<OptionItemUIBase> _itemUIs = new List<OptionItemUIBase>();

        // カーソル位置。0〜(_itemUIs.Count-1)は各設定項目、_itemUIs.Countは閉じるボタンを示す
        private int _selectedIndex = 0;

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
        /// オプション画面を表示し、カーソルを先頭項目にリセットします
        /// </summary>
        public void Show()
        {
            _optionUI.gameObject.SetActive( true );

            _selectedIndex = 0;
            UpdateSelectionVisuals();
        }

        /// <summary>
        /// オプション画面を非表示にします
        /// </summary>
        public void Hide()
        {
            _optionUI.gameObject.SetActive( false );
        }

        /// <summary>
        /// カーソルを上下に移動します(末尾・先頭で循環します)
        /// </summary>
        /// <param name="delta">移動量(-1で前の項目、+1で次の項目)</param>
        public void MoveSelection( int delta )
        {
            int count = _itemUIs.Count + 1; // +1 は閉じるボタンの分
            _selectedIndex = ( _selectedIndex + delta + count ) % count;

            UpdateSelectionVisuals();
        }

        /// <summary>
        /// 現在選択中の項目を確定操作します(トグルの切替・選択肢の切替・閉じるボタンの実行)。
        /// スライダー選択時は何も行いません(左右入力で調整するため)
        /// </summary>
        public void ConfirmSelection()
        {
            if( _selectedIndex == _itemUIs.Count )
            {
                OnCloseRequested?.Invoke();
                return;
            }

            switch( _itemUIs[_selectedIndex] )
            {
                case ToggleOptionItemUI toggleUI:
                    toggleUI.Toggle();
                    break;

                case ChoiceOptionItemUI choiceUI:
                    choiceUI.Step( 1 );
                    break;
            }
        }

        /// <summary>
        /// 現在選択中の項目がスライダーであれば値をdelta分調整し、選択肢であればdelta分選択肢をずらします
        /// </summary>
        public void AdjustSelected( int delta )
        {
            if( _selectedIndex >= _itemUIs.Count ) { return; }

            switch( _itemUIs[_selectedIndex] )
            {
                case SliderOptionItemUI sliderUI:
                    sliderUI.AdjustValue( delta );
                    break;

                case ChoiceOptionItemUI choiceUI:
                    choiceUI.Step( delta );
                    break;
            }
        }

        /// <summary>
        /// 現在選択中の項目が、左右入力で値を調整できる項目(スライダー・選択肢)かどうかを返します
        /// </summary>
        public bool IsHorizontalAdjustableSelected()
        {
            if( _selectedIndex >= _itemUIs.Count ) { return false; }

            return _itemUIs[_selectedIndex] is SliderOptionItemUI || _itemUIs[_selectedIndex] is ChoiceOptionItemUI;
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

                    case ChoiceOptionItem choiceItem:
                        BuildChoiceItemUI( choiceItem );
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

            _itemUIs.Add( itemUI );
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

            _itemUIs.Add( itemUI );
        }

        private void BuildChoiceItemUI( ChoiceOptionItem item )
        {
            var itemUI = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<ChoiceOptionItemUI>( _prefabReg.ChoiceOptionItemPrefab, true, false, item.Label );
            itemUI.Setup();
            itemUI.gameObject.SetActive( true );
            itemUI.transform.SetParent( _optionUI.ContentRoot, false );

            itemUI.SetLabel( item.Label );
            itemUI.SetChoices( item.Choices );
            itemUI.SetIndexWithoutNotify( item.CurrentIndex );
            itemUI.OnValueChanged += item.Apply;

            _itemUIs.Add( itemUI );
        }

        /// <summary>
        /// 現在のカーソル位置を各項目・閉じるボタンの表示に反映します
        /// </summary>
        private void UpdateSelectionVisuals()
        {
            for( int i = 0; i < _itemUIs.Count; ++i )
            {
                _itemUIs[i].SetSelected( i == _selectedIndex );
            }

            _optionUI.SetCloseSelected( _selectedIndex == _itemUIs.Count );
        }
    }
}
