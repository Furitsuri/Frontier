using UnityEngine;
using UnityEngine.UI;

namespace Frontier.UI
{
    /// <summary>
    /// オプション画面の最小限のView。
    /// 個々の設定項目UIを並べるコンテナと閉じるボタンの参照を公開するのみで、
    /// 項目の生成・操作はOptionPresenterが担います。
    /// </summary>
    public class OptionUI : UiMonoBehaviour
    {
        [SerializeField] private Transform _contentRoot;
        [SerializeField] private Button _closeButton;

        public Transform ContentRoot => _contentRoot;
        public Button CloseButton => _closeButton;
    }
}
