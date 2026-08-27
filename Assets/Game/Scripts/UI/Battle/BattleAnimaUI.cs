using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Frontier.UI
{
    /// <summary>
    /// 戦闘中に撃破した敵から獲得したアニマを常時表示するView。
    /// 戦闘開始時に0リセットされる戦闘専用の累計値であり、戦闘開始前から所持していたアニマ
    /// (UserDomain.Anima)は含まない。入力ガイドバーの左端付近に配置する想定。
    /// </summary>
    public class BattleAnimaUI : UIMonoBehaviourIncludingText
    {
        [SerializeField] private TextMeshProUGUI _labelText;
        [SerializeField] private TextMeshProUGUI _animaText;
        // 加算値ポップアップ("+10"等)のテンプレート兼第1インスタンス。ウィンドウ右上に配置する想定
        [SerializeField] private TextMeshProUGUI _addedValueTemplate;

        // 加算(正の値)は赤、消費(負の値、将来のアニマ消費コマンド用)は青で表示する
        private static readonly Color AddedValuePositiveColor = Color.red;
        private static readonly Color AddedValueNegativeColor = Color.blue;

        private class AddedValuePopup
        {
            public TextMeshProUGUI Text;
            public float Elapsed;
        }

        // 現在表示中のポップアップ(古いものが先頭、新しいものが末尾=一番上に積まれる)
        private List<AddedValuePopup> _activePopups = new List<AddedValuePopup>();
        // 生成済みのポップアップ用テキストインスタンス一覧(非アクティブなものを使い回すプールとして管理する)
        private List<TextMeshProUGUI> _popupPool = new List<TextMeshProUGUI>();
        private Vector2 _popupBasePosition;

        /// <summary>
        /// UiMonoBehaviour.Setup()の既定実装はgameObjectを非表示にするため、
        /// 常時表示としたいこのUIではSetup完了時点で明示的に表示状態へ戻す。
        /// SetActive(true)によりOnEnable()が呼ばれ、ラベルのローカライズ文言も併せて反映される。
        /// </summary>
        public override void Setup()
        {
            base.Setup();

            _popupBasePosition = ( ( RectTransform ) _addedValueTemplate.transform ).anchoredPosition;
            _addedValueTemplate.gameObject.SetActive( false );

            gameObject.SetActive( true );
        }

        public void SetAnima( int anima )
        {
            _animaText.text = anima.ToString();
        }

        /// <summary>
        /// 加算値のポップアップ("+10"等)を、現在表示中のものより上に積み上げる形で表示します。
        /// 正の値は赤(獲得)、負の値は青(将来の消費コマンド用)で表示し、一定時間経過後に自動で消えます。
        /// </summary>
        public void ShowAddedValuePopup( int amount )
        {
            var text = GetOrCreatePopupText();
            text.text  = ( 0 <= amount ? "+" : "" ) + amount;
            text.color = 0 <= amount ? AddedValuePositiveColor : AddedValueNegativeColor;
            text.gameObject.SetActive( true );

            _activePopups.Add( new AddedValuePopup { Text = text, Elapsed = 0f } );
            RepositionPopups();
        }

        /// <summary>
        /// いずれかの加算値ポップアップが表示中かどうかを取得します。
        /// ステージクリア演出は、この表示が終わるまで開始を待つために使用します。
        /// </summary>
        public bool IsShowingAddedValuePopup => 0 < _activePopups.Count;

        void Update()
        {
            for ( int i = _activePopups.Count - 1; 0 <= i; --i )
            {
                _activePopups[i].Elapsed += DeltaTimeProvider.DeltaTime;
                if ( Constants.ANIMA_ADDED_VALUE_POPUP_DURATION <= _activePopups[i].Elapsed )
                {
                    _activePopups[i].Text.gameObject.SetActive( false );
                    _activePopups.RemoveAt( i );
                }
            }

            RepositionPopups();
        }

        /// <summary>
        /// 表示中のポップアップを、古いものほど下・新しいものほど上になるよう並べ直します
        /// </summary>
        private void RepositionPopups()
        {
            for ( int i = 0; i < _activePopups.Count; ++i )
            {
                var rect = ( RectTransform ) _activePopups[i].Text.transform;
                rect.anchoredPosition = _popupBasePosition + new Vector2( 0f, i * Constants.ANIMA_ADDED_VALUE_POPUP_STACK_SPACING );
            }
        }

        /// <summary>
        /// 非アクティブなポップアップ用テキストを1つ返します。無ければテンプレートから新規生成します
        /// </summary>
        private TextMeshProUGUI GetOrCreatePopupText()
        {
            foreach ( var text in _popupPool )
            {
                if ( !text.gameObject.activeSelf ) { return text; }
            }

            var newText = Instantiate( _addedValueTemplate, _addedValueTemplate.transform.parent );
            _popupPool.Add( newText );
            return newText;
        }

        /// <summary>
        /// ステージクリア時など、この表示を明示的に隠したい場面で呼び出します。
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive( false );
        }

        #region ILocalizedText implementation

        public override void RefreshText()
        {
            _labelText.text = _localization.Get( _textKey );
        }

        #endregion  // ILocalizedText implementation
    }
}
