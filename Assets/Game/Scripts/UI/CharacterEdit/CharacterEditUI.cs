using Frontier.Entities;
using TMPro;
using UnityEngine;
using Zenject;

namespace Frontier.UI
{
    /// <summary>
    /// キャラクター編集画面の見た目のみを担当するView。左側にメニュー(レベルアップ/装備スキル設定)、
    /// 上部に編集対象キャラクターのパラメータ表示(Lv.名前・HP/ATK/DEF/MOV/JMP/ACT/スキル)を表示する。
    /// レベルアップ/装備スキル設定の実際の内容は、右側に重ねて表示される別パネル(LevelUpUI/SkillEquipUI、
    /// 未実装)が担う想定で、このメニュー自体は画面遷移後も表示したままにする(選択中項目のハイライトは維持される)。
    /// 開閉や選択状態の管理はCharacterEditPresenterが行い、このクラスは表示指示を受けて
    /// 反映するだけに留める。
    /// </summary>
    public class CharacterEditUI : UiMonoBehaviour, ISelectableMenuView
    {
        private static readonly string[] MenuItemLocalizationKeys =
        {
            "UI_CMD_LEVEL_UP",     // LEVEL_UP
            "UI_CMD_STATUS_UP",    // STATUS_UP
            "UI_CMD_SKILL_EQUIP",  // SKILL_EQUIP
        };

        [Header( "上部キャラクターパラメータ表示(HP/ATK/DEF/MOV/JMP/ACT/スキル)" )]
        [SerializeField] private CharacterParameterUI _characterParamUI;

        [Header( "パラメータ表示内の「Lv.名前」ヘッダーテキスト" )]
        [SerializeField] private TextMeshProUGUI _characterParamNameText;

        [Header( "左メニュー項目テキスト(CHARACTER_EDIT_MENU_OPTION_TAGの並び順と一致させること)" )]
        [SerializeField] private TextMeshProUGUI[] _menuItemTexts;

        [Header( "所持金テキスト(TroopEdit画面と同じ位置に表示する)" )]
        [SerializeField] private TextMeshProUGUI _moneyText;

        [Header( "部隊共有経験値(仮称)テキスト(TroopEdit画面と同じ位置に表示する)" )]
        [SerializeField] private TextMeshProUGUI _expText;

        [Header( "レベルアップ項目選択時に右側へ表示するレベルアップ画面" )]
        [SerializeField] private LevelUpUI _levelUpView;

        [Header( "ステータス上昇項目選択時に右側へ表示するステータス上昇画面" )]
        [SerializeField] private StatusUpUI _statusUpView;

        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _selectedColor = Color.red;
        [SerializeField] private Color _lockedColor = Color.gray;

        [Inject] private ILocalizationService _localization = null;

        public CharacterParameterUI CharacterParamUI => _characterParamUI;
        public LevelUpUI LevelUpView => _levelUpView;
        public StatusUpUI StatusUpView => _statusUpView;

        public override void Setup()
        {
            base.Setup();

            RefreshMenuTexts();
            _characterParamUI?.Setup();
            _levelUpView?.Setup();
            _statusUpView?.Setup();

            if ( _localization != null ) { _localization.OnLanguageChanged += RefreshMenuTexts; }
        }

        private void OnDestroy()
        {
            if ( _localization != null ) { _localization.OnLanguageChanged -= RefreshMenuTexts; }
        }

        public void Show() => gameObject.SetActive( true );

        public void Hide() => gameObject.SetActive( false );

        /// <summary>
        /// 選択中の項目インデックスを表示(色)に反映します(ISelectableMenuView実装)。
        /// ロック状態(SetLockedIndex)の解除にも使用します。
        /// </summary>
        public void SetSelectedIndex( int index )
        {
            for ( int i = 0; i < _menuItemTexts.Length; ++i )
            {
                _menuItemTexts[i].color = ( i == index ) ? _selectedColor : _normalColor;
            }
        }

        /// <summary>
        /// activeIndexの項目が確定されている間、他の項目をグレーアウトします(ISelectableMenuView実装)。
        /// 表現方法(現状は文字色)を変更したくなった場合は、この実装のみを差し替えれば良い。
        /// </summary>
        public void SetLockedIndex( int activeIndex )
        {
            for ( int i = 0; i < _menuItemTexts.Length; ++i )
            {
                _menuItemTexts[i].color = ( i == activeIndex ) ? _selectedColor : _lockedColor;
            }
        }

        /// <summary>
        /// パラメータ表示内の「Lv.名前」ヘッダーを更新します。L1/R1でキャラクターを切り替えた際に呼ばれます。
        /// </summary>
        public void SetCharacterInfo( Character character )
        {
            if ( _characterParamNameText == null ) return;

            var status = character.GetStatusRef;
            _characterParamNameText.text = $"Lv.{status.Level}  {status.Name}";
        }

        /// <summary>
        /// 画面右上の所持金・部隊共有経験値(仮称)表示を更新します(TroopEdit画面と同じ位置)。
        /// </summary>
        public void SetHeaderInfo( int money, int exp )
        {
            _moneyText.text = money.ToString();
            _expText.text = exp.ToString();
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
