using TMPro;
using UnityEngine;

namespace Frontier.UI
{
    /// <summary>
    /// ステージクリア時に画面の広範囲へ表示するリザルト画面です。
    /// 表示内容はまだ確定していないため、現状は今回の戦闘で獲得した総アニマ量のみを表示します。
    /// </summary>
    public class StageResultUI : UiMonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _animaValueText;

        public void SetAnima( int anima )
        {
            _animaValueText.text = anima.ToString();
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
