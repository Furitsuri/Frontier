using TMPro;
using UnityEngine;

namespace Frontier.UI
{
    /// <summary>
    /// オプション項目UIの最小限の共通基底クラス。
    /// ラベル表示のみを共通機能として持ち、値の種類ごとの表示・操作は派生クラスが担います。
    /// </summary>
    public abstract class OptionItemUIBase : UiMonoBehaviour
    {
        [SerializeField] protected TextMeshProUGUI _label;

        public void SetLabel( string text )
        {
            _label.text = text;
        }
    }
}
