using TMPro;
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
        [SerializeField] private TextMeshProUGUI _closeButtonText;

        public Transform ContentRoot => _contentRoot;
        public Button CloseButton => _closeButton;

        /// <summary>
        /// カーソルによる選択状態を閉じるボタンの表示に反映します
        /// </summary>
        public void SetCloseSelected( bool isSelected )
        {
            _closeButtonText.color = isSelected ? Color.red : Color.white;
        }
    }
}
