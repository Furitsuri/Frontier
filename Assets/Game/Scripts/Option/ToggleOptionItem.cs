using System;

namespace Frontier.Option
{
    /// <summary>
    /// ON/OFFを切り替えるオプション項目。
    /// 現時点では実データを持つ項目は存在しませんが、SliderOptionItemと同じ枠組みで
    /// 追加できるようにするための型です。
    /// </summary>
    public class ToggleOptionItem : IOptionItem
    {
        public string Label { get; }
        public bool CurrentValue { get; private set; }

        private readonly Action<bool> _onApply;

        public ToggleOptionItem( string label, bool initialValue, Action<bool> onApply )
        {
            Label = label;
            CurrentValue = initialValue;
            _onApply = onApply;
        }

        /// <summary>
        /// 値の変更を確定し、適用処理を呼び出します
        /// </summary>
        public void Apply( bool value )
        {
            CurrentValue = value;
            _onApply?.Invoke( value );
        }
    }
}
