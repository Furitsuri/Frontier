using Frontier.UI;
using System;
using Zenject;

namespace Frontier.Option
{
    /// <summary>
    /// オプション画面の表示・操作を担うPresenter。
    /// OptionUIが公開する部品を直接操作し、機能をOptionHandlerに提供します。
    /// </summary>
    public class OptionPresenter
    {
        [Inject] private IUiSystem _uiSystem = null;

        private OptionUI _optionUI = null;

        public event Action OnCloseRequested;
        public event Action<float> OnBgmVolumeChanged;
        public event Action<float> OnSeVolumeChanged;

        /// <summary>
        /// 初期化を行います
        /// </summary>
        public void Init()
        {
            _optionUI = _uiSystem.GeneralUi.OptionView;
            _optionUI.Setup();

            _optionUI.CloseButton.onClick.AddListener( () => OnCloseRequested?.Invoke() );
            _optionUI.BgmSlider.onValueChanged.AddListener( value => OnBgmVolumeChanged?.Invoke( value ) );
            _optionUI.SeSlider.onValueChanged.AddListener( value => OnSeVolumeChanged?.Invoke( value ) );
        }

        /// <summary>
        /// オプション画面を表示します
        /// </summary>
        /// <param name="bgmVolume">表示するBGM音量</param>
        /// <param name="seVolume">表示するSE音量</param>
        public void Show( float bgmVolume, float seVolume )
        {
            _optionUI.gameObject.SetActive( true );

            _optionUI.BgmSlider.SetValueWithoutNotify( bgmVolume );
            _optionUI.SeSlider.SetValueWithoutNotify( seVolume );
        }

        /// <summary>
        /// オプション画面を非表示にします
        /// </summary>
        public void Hide()
        {
            _optionUI.gameObject.SetActive( false );
        }
    }
}
