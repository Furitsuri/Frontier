using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Frontier.UI
{
    /// <summary>
    /// 話者名・セリフ・ポートレートを表示する汎用トークウィンドウの見た目のみを担当する最小限のビュー。
    /// 表示内容の決定(どのシーンのどの発言を表示するか)は GeneralUISystem.ShowTalk() が行い、
    /// このクラスは受け取った内容を反映するだけに留める。
    /// </summary>
    public class TalkWindowUI : UiMonoBehaviour
    {
        [Header( "話者名" )]
        [SerializeField] private TextMeshProUGUI _speakerNameText;

        [Header( "セリフ本文" )]
        [SerializeField] private TextMeshProUGUI _messageText;

        [Header( "ポートレート画像" )]
        [SerializeField] private Image _portraitImage;

        public override void Setup()
        {
            base.Setup();
        }

        /// <summary>
        /// 話者名・セリフ・ポートレートを設定して表示します。
        /// portraitがnullの場合はポートレート画像を非表示にします。
        /// </summary>
        public void Show( string speakerName, string message, Sprite portrait )
        {
            _speakerNameText.text = speakerName;
            _messageText.text     = message;

            _portraitImage.enabled = ( portrait != null );
            _portraitImage.sprite  = portrait;

            gameObject.SetActive( true );
        }

        public void Hide()
        {
            gameObject.SetActive( false );
        }
    }
}
