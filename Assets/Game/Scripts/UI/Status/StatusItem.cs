using UnityEngine;
using TMPro;

namespace Frontier.UI
{
    public sealed class StatusItem : UiMonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _valueText;

        public void SetValueText( string text )
        {
            if( null != _valueText )
            {
                _valueText.text = text;
            }
        }
    }
}