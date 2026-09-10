using TMPro;
using UnityEngine;
using Zenject;

namespace Frontier.UI
{
    /// <summary>
    /// 画面上部の全幅に常時表示する所持アニマ・部隊人数(現在数/上限数)のHUD。
    /// GeneralUI.prefab側で管理される共通UIであり、FieldScene/RecruitScene等、
    /// GeneralUIを利用する全シーンで共通して表示される。
    /// 左端には、現在どの画面(State)を表示しているかを示すタイトルテキストも持つ
    /// (雇用/解雇/部隊編集画面等、TroopEdit系の画面がここへタイトルを設定する)。
    /// </summary>
    public class GeneralHeaderUI : UiMonoBehaviour
    {
        [Header( "所持アニマのアイコン右側に表示する数値テキスト" )]
        [SerializeField] private TextMeshProUGUI _animaValueText;

        [Header( "部隊人数(現在数/上限数)テキスト" )]
        [SerializeField] private TextMeshProUGUI _memberCountText;

        [Header( "左端に表示する現在の画面(State)タイトルテキスト" )]
        [SerializeField] private TextMeshProUGUI _stateTitleText;

        [Inject] private ILocalizationService _localization = null;

        private LocKey? _stateTitleKey = null;

        public override void Setup()
        {
            base.Setup();

            if ( _localization != null ) { _localization.OnLanguageChanged += RefreshStateTitleText; }
        }

        private void OnDestroy()
        {
            if ( _localization != null ) { _localization.OnLanguageChanged -= RefreshStateTitleText; }
        }

        public void Show() => gameObject.SetActive( true );

        public void Hide() => gameObject.SetActive( false );

        public void SetHeaderInfo( int anima, int currentMemberNum, int maxMemberNum )
        {
            _animaValueText.text = anima.ToString();
            _memberCountText.text = $"{currentMemberNum}/{maxMemberNum}";
        }

        /// <summary>
        /// 左端に現在の画面(State)タイトルを表示します(雇用/解雇/部隊編集画面等)。
        /// </summary>
        public void SetStateTitle( LocKey key )
        {
            _stateTitleKey = key;
            RefreshStateTitleText();
        }

        /// <summary>
        /// 左端の画面タイトル表示を消去します(該当する画面を離れた際に呼び出してください)。
        /// </summary>
        public void ClearStateTitle()
        {
            _stateTitleKey = null;
            RefreshStateTitleText();
        }

        private void RefreshStateTitleText()
        {
            if ( _stateTitleText == null ) return;

            _stateTitleText.text = _stateTitleKey.HasValue
                ? ( _localization != null ? _localization.Get( _stateTitleKey.Value ) : _stateTitleKey.Value.ToString() )
                : string.Empty;
        }
    }
}
