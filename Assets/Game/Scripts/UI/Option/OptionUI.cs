using UnityEngine;
using UnityEngine.UI;

namespace Frontier.UI
{
    /// <summary>
    /// オプション画面の最小限のView。
    /// 個々のUI部品の参照を公開するのみで、それらを用いた機能の提供はOptionPresenterが担います。
    /// </summary>
    public class OptionUI : UiMonoBehaviour
    {
        [SerializeField] private Slider _bgmSlider;
        [SerializeField] private Slider _seSlider;
        [SerializeField] private Button _closeButton;

        public Slider BgmSlider => _bgmSlider;
        public Slider SeSlider => _seSlider;
        public Button CloseButton => _closeButton;
    }
}
