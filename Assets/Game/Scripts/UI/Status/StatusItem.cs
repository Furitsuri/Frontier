using UnityEngine;
using TMPro;

namespace Frontier.UI
{
    public sealed class StatusItem : UIMonoBehaviourIncludingText, ITooltipContent
    {
        [SerializeField] private TextMeshProUGUI _valueText;
        [SerializeField] private TextMeshProUGUI _bonusText;   // 基礎値に加算されている補正値(「+ 1」等)。基礎値の右隣に表示する。文字色はPrefab側で設定

        private string _tooltipText = string.Empty;
        private string _tooltipDetailText = string.Empty;  // 説明文の下に追記する内容(補正値の内訳等)

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

        /// <summary>
        /// 補正値の表示を設定します。空文字列の場合は非表示にします。
        /// </summary>
        public void SetBonusText( string text )
        {
            if( null == _bonusText ) { return; }

            _bonusText.text = text;
            _bonusText.gameObject.SetActive( !string.IsNullOrEmpty( text ) );
        }

        public void SetTooltipText( LocKey text )
        {
            _tooltipText = _localization.Get( _textKey );
        }

        /// <summary>
        /// ツールチップの説明文の下に、1行空けて追記する内容を設定します。空文字列の場合は追記しません。
        /// </summary>
        public void SetTooltipDetailText( string text )
        {
            _tooltipDetailText = text ?? string.Empty;
        }

        public string GetTooltipText()
        {
            if( string.IsNullOrEmpty( _tooltipDetailText ) ) { return _tooltipText; }

            return $"{_tooltipText}\n\n{_tooltipDetailText}";
        }

        public RectTransform GetRectTransform()
        {
            return this.GetComponent<RectTransform>();
        }

#region ILocalizedText implementation

        public override void RefreshText()
        {
            _tooltipText = _localization.Get( _textKey );
        }

#endregion  // ILocalizedText implementation
    }
}