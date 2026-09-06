using TMPro;
using UnityEngine;
using Zenject;

namespace Frontier.UI
{
    /// <summary>
    /// 雇用フェーズ開始時に表示する「雇用/解雇」選択メニューの見た目のみを担当する最小限のビュー。
    /// 開閉・カーソル位置・確定処理などの状態管理は RecruitTopMenuState / RecruitPhasePresenter が行い、
    /// このクラスは表示指示(Show/Hide/SetSelectedIndex)を受けて反映するだけに留める。
    /// GameObject階層はシーン側(RecruitmentUI配下)にデザイナーが確認・編集できる形で用意し、
    /// 各項目テキストは _optionTexts にInspectorから割り当てる。
    /// </summary>
    public class RecruitTopMenuUI : MonoBehaviour
    {
        // RECRUIT_TOP_MENU_OPTION_TAG の並び順と対応させること
        private static readonly LocKey[] OptionTextKeys =
        {
            LocKey.UI_CMD_EMPLOY,   // EMPLOY
            LocKey.UI_CMD_DISMISS,  // DISMISS
        };

        [Header( "項目テキスト(RECRUIT_TOP_MENU_OPTION_TAGの並び順と対応させること)" )]
        [SerializeField] private TextMeshProUGUI[] _optionTexts;

        [Header( "カーソル色" )]
        [SerializeField] private Color _normalColor   = Color.white;
        [SerializeField] private Color _selectedColor = Color.red;

        [Inject] private ILocalizationService _localization = null;

        public void Init()
        {
            RefreshAllTexts();

            if ( _localization != null ) { _localization.OnLanguageChanged += RefreshAllTexts; }
        }

        private void OnDestroy()
        {
            if ( _localization != null ) { _localization.OnLanguageChanged -= RefreshAllTexts; }
        }

        public void Show() => gameObject.SetActive( true );

        public void Hide() => gameObject.SetActive( false );

        /// <summary>選択中の項目インデックスを表示(色)に反映します。</summary>
        public void SetSelectedIndex( int index )
        {
            for ( int i = 0; i < _optionTexts.Length; ++i )
            {
                _optionTexts[i].color = ( i == index ) ? _selectedColor : _normalColor;
            }
        }

        /// <summary>
        /// 言語切替時に、表示中の全項目のテキストを現在の言語で再取得します。
        /// </summary>
        private void RefreshAllTexts()
        {
            for ( int i = 0; i < _optionTexts.Length; ++i )
            {
                _optionTexts[i].text = _localization.Get( OptionTextKeys[i] );
            }
        }
    }
}
