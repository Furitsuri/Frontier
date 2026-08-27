using TMPro;
using UnityEngine;
using Zenject;

namespace Frontier.UI
{
    /// <summary>
    /// ステージクリア時に画面の広範囲へ表示するリザルト画面です。
    /// 表示内容はまだ確定していないため、現状は今回の戦闘で獲得した総アニマ量とクリアまでのターン数のみを表示します。
    /// </summary>
    public class StageResultUI : UiMonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _turnLabelText;
        [SerializeField] private TextMeshProUGUI _turnValueText;
        [SerializeField] private TextMeshProUGUI _animaLabelText;
        [SerializeField] private TextMeshProUGUI _animaValueText;

        [Inject] private ILocalizationService _localization = null;

        public override void Setup()
        {
            base.Setup();

            RefreshLabels();
            _localization.OnLanguageChanged += RefreshLabels;
        }

        private void OnDestroy()
        {
            _localization.OnLanguageChanged -= RefreshLabels;
        }

        /// <summary>
        /// タイトル・各項目名のローカライズ文言を現在の言語で反映します
        /// (値そのもの(SetAnima/SetTurnCount)はここでは触りません)。
        /// </summary>
        private void RefreshLabels()
        {
            _titleText.text     = _localization.Get( LocKey.UI_STAGE_RESULT_TITLE );
            _turnLabelText.text = _localization.Get( LocKey.UI_STAGE_RESULT_TURN );
            _animaLabelText.text = _localization.Get( LocKey.UI_BATTLE_ANIMA );
        }

        public void SetAnima( int anima )
        {
            _animaValueText.text = anima.ToString();
        }

        public void SetTurnCount( int turnCount )
        {
            _turnValueText.text = turnCount.ToString();
        }

        public void Show()
        {
            gameObject.SetActive( true );
        }

        public void Hide()
        {
            gameObject.SetActive( false );
        }
    }
}
