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

        [Header( "所持アニマ増減差分テキスト(アニマ数値のすぐ下に表示。差分0の場合は非表示)" )]
        [SerializeField] private TextMeshProUGUI _animaDiffText;

        [Header( "所持アニマ増減差分を強調表示する際の文字サイズと、通常位置からの縦のずらし量(大きくなった分、数値との重なりを避ける)" )]
        [SerializeField] private float _animaDiffEmphasizedFontSize = 36f;
        [SerializeField] private float _animaDiffEmphasizedOffsetY  = -10f;

        [Header( "部隊人数(現在数/上限数)テキスト" )]
        [SerializeField] private TextMeshProUGUI _memberCountText;

        [Header( "左端に表示する現在の画面(State)タイトルテキスト" )]
        [SerializeField] private TextMeshProUGUI _stateTitleText;

        [Inject] private ILocalizationService _localization = null;

        private LocKey?  _stateTitleKey  = null;
        private object[] _stateTitleArgs = null;

        // 増減差分テキストの通常時の文字サイズ・位置(初回に控えておき、強調表示の解除時に戻す)
        private bool    _isAnimaDiffDefaultCached = false;
        private float   _animaDiffDefaultFontSize = 0f;
        private Vector2 _animaDiffDefaultPosition = Vector2.zero;

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
        /// 所持アニマの増減差分を、アニマ数値のすぐ下に表示します(雇用/解雇画面での予備登録による
        /// 増減量を想定)。0を渡すと非表示にします。正の値は"+"付きで緑色、負の値は赤色で表示します。
        /// emphasizedをtrueにすると、文字を大きくして目立たせます(購入確認画面等、金額の増減が主役の場面向け)。
        /// 強調の指定は呼び出しごとに反映されるため、毎フレーム呼ぶ側は常に現在の状態を渡してください。
        /// </summary>
        public void SetAnimaDiff( int diff, bool emphasized = false )
        {
            if( _animaDiffText == null ) return;

            ApplyAnimaDiffEmphasis( emphasized );

            if( diff == 0 )
            {
                _animaDiffText.text = string.Empty;
                return;
            }

            _animaDiffText.text  = diff > 0 ? $"+{diff}" : diff.ToString();
            _animaDiffText.color = diff > 0 ? Color.green : Color.red;
        }

        private void ApplyAnimaDiffEmphasis( bool emphasized )
        {
            if( !_isAnimaDiffDefaultCached )
            {
                _isAnimaDiffDefaultCached = true;
                _animaDiffDefaultFontSize = _animaDiffText.fontSize;
                _animaDiffDefaultPosition = _animaDiffText.rectTransform.anchoredPosition;
            }

            // 雇用/解雇画面のように毎フレーム呼ばれるため、値が変わる場合のみ書き込む
            float fontSize = emphasized ? _animaDiffEmphasizedFontSize : _animaDiffDefaultFontSize;
            if( !Mathf.Approximately( _animaDiffText.fontSize, fontSize ) ) { _animaDiffText.fontSize = fontSize; }

            Vector2 position = emphasized
                ? _animaDiffDefaultPosition + new Vector2( 0f, _animaDiffEmphasizedOffsetY )
                : _animaDiffDefaultPosition;
            if( _animaDiffText.rectTransform.anchoredPosition != position ) { _animaDiffText.rectTransform.anchoredPosition = position; }
        }

        /// <summary>
        /// 左端に現在の画面(State)タイトルを表示します(雇用/解雇/部隊編集/ステージ番号表示等)。
        /// 文言に書式指定(例: "ステージ{0}")が含まれる場合は、argsをstring.Formatで挿入します。
        /// </summary>
        public void SetStateTitle( LocKey key, params object[] args )
        {
            _stateTitleKey  = key;
            _stateTitleArgs = args;
            RefreshStateTitleText();
        }

        /// <summary>
        /// 左端の画面タイトル表示を消去します(該当する画面を離れた際に呼び出してください)。
        /// </summary>
        public void ClearStateTitle()
        {
            _stateTitleKey  = null;
            _stateTitleArgs = null;
            RefreshStateTitleText();
        }

        private void RefreshStateTitleText()
        {
            if ( _stateTitleText == null ) return;

            if ( !_stateTitleKey.HasValue )
            {
                _stateTitleText.text = string.Empty;
                return;
            }

            string format = _localization != null ? _localization.Get( _stateTitleKey.Value ) : _stateTitleKey.Value.ToString();
            _stateTitleText.text = ( _stateTitleArgs != null && _stateTitleArgs.Length > 0 )
                ? string.Format( format, _stateTitleArgs )
                : format;
        }
    }
}
