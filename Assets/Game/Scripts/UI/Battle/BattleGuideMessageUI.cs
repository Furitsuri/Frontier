using TMPro;
using UnityEngine;
using Zenject;

namespace Frontier.UI
{
    /// <summary>
    /// 画面中央上部にウィンドウ付きで表示する、現在の操作状態をユーザーへ伝えるための案内文言UIです。
    /// (例: グループ移動のメンバー選択中であることの案内)
    /// 表示中に言語が切り替わった場合は、表示中の文言を現在の言語で再表示します。
    /// </summary>
    public class BattleGuideMessageUI : UiMonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _messageText;

        [Inject] private ILocalizationService _localization = null;

        private LocKey _messageKey = LocKey.None;

        public override void Setup()
        {
            base.Setup();

            _localization.OnLanguageChanged += RefreshText;
        }

        private void OnDestroy()
        {
            _localization.OnLanguageChanged -= RefreshText;
        }

        /// <summary>
        /// 指定の文言で案内を表示します
        /// </summary>
        public void Show( LocKey messageKey )
        {
            _messageKey = messageKey;
            RefreshText();
            gameObject.SetActive( true );
        }

        /// <summary>
        /// 案内を非表示にします
        /// </summary>
        public void Hide()
        {
            _messageKey = LocKey.None;
            gameObject.SetActive( false );
        }

        private void RefreshText()
        {
            if( LocKey.None == _messageKey ) { return; }

            _messageText.text = _localization.Get( _messageKey );
        }
    }
}
