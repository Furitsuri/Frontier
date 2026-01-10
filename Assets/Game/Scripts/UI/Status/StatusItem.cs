using UnityEngine;
using TMPro;

namespace Frontier.UI
{
    public sealed class StatusItem : UIMonoBehaviourIncludingText, ITooltipContent
    {
        [SerializeField] private TextMeshProUGUI _valueText;

        private string _tooltipText = string.Empty;

        /// <summary>
        /// OnEnableでEnableRefreshText()を呼び出さないため、空実装にしています。
        /// </summary>
        void OnEnable() { }

        public void SetValueText( string text )
        {
            if( null != _valueText )
            {
                _valueText.text = text;
            }

            EnableRefreshText();
        }

        public void SetTooltipText( string text )
        {
            _tooltipText = _localization.Get( _textKey );
        }

        public string GetTooltipText()
        {
            return _tooltipText;
        }

        public RectTransform GetRectTransform()
        {
            return this.GetComponent<RectTransform>();
        }

#region ILocalizedText implementation

        override public void RefreshText()
        {
            _tooltipText = _localization.Get( _textKey );
        }

#endregion  // ILocalizedText implementation
    }
}