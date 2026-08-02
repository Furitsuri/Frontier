using Frontier.Entities;
using TMPro;
using UnityEngine;
using Zenject;

namespace Frontier.UI
{
    /// <summary>
    /// キャラクター編集画面の見た目のみを担当するView。左側にメニュー(レベルアップ/装備スキル設定)、
    /// 上部に編集対象キャラクターの簡易情報(Lv.名前)を表示する。レベルアップ/装備スキル設定の
    /// 実際の内容は、右側に重ねて表示される別パネル(LevelUpUI/SkillEquipUI、未実装)が担う想定で、
    /// このメニュー自体は画面遷移後も表示したままにする(選択中項目のハイライトは維持される)。
    /// 開閉や選択状態の管理はCharacterEditPresenterが行い、このクラスは表示指示を受けて
    /// 反映するだけに留める。
    /// </summary>
    public class CharacterEditUI : UiMonoBehaviour
    {
        private static readonly string[] MenuItemLocalizationKeys =
        {
            "UI_CMD_LEVEL_UP",     // LEVEL_UP
            "UI_CMD_SKILL_EQUIP",  // SKILL_EQUIP
        };

        [Header( "上部キャラクター情報(Lv.名前)テキスト" )]
        [SerializeField] private TextMeshProUGUI _characterInfoText;

        [Header( "左メニュー項目テキスト(CHARACTER_EDIT_MENU_OPTION_TAGの並び順と一致させること)" )]
        [SerializeField] private TextMeshProUGUI[] _menuItemTexts;

        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _selectedColor = Color.red;

        [Inject] private ILocalizationService _localization = null;

        public override void Setup()
        {
            base.Setup();

            RefreshMenuTexts();

            if ( _localization != null ) { _localization.OnLanguageChanged += RefreshMenuTexts; }
        }

        private void OnDestroy()
        {
            if ( _localization != null ) { _localization.OnLanguageChanged -= RefreshMenuTexts; }
        }

        public void Show() => gameObject.SetActive( true );

        public void Hide() => gameObject.SetActive( false );

        /// <summary>
        /// 選択中の項目インデックスを表示(色)に反映します。
        /// サブ画面(レベルアップ等)を開いている間もこの値を変更しなければ、ハイライトは維持されます。
        /// </summary>
        public void SetSelectedIndex( int index )
        {
            for ( int i = 0; i < _menuItemTexts.Length; ++i )
            {
                _menuItemTexts[i].color = ( i == index ) ? _selectedColor : _normalColor;
            }
        }

        /// <summary>
        /// 上部のキャラクター情報(Lv.名前)を更新します。L1/R1でキャラクターを切り替えた際に呼ばれます。
        /// </summary>
        public void SetCharacterInfo( Character character )
        {
            if ( _characterInfoText == null ) return;

            var status = character.GetStatusRef;
            _characterInfoText.text = $"Lv.{status.Level}  {status.Name}";
        }

        private void RefreshMenuTexts()
        {
            for ( int i = 0; i < _menuItemTexts.Length; ++i )
            {
                _menuItemTexts[i].text = _localization != null ? _localization.Get( MenuItemLocalizationKeys[i] ) : MenuItemLocalizationKeys[i];
            }
        }
    }
}
