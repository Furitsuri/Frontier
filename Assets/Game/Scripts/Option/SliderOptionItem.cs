using System;

namespace Frontier.Option
{
    /// <summary>
    /// スライダーで値を編集するオプション項目(BGM/SE音量など)。
    /// </summary>
    public class SliderOptionItem : IOptionItem
    {
        public string Label { get; }
        public float MinValue { get; }
        public float MaxValue { get; }
        public float CurrentValue { get; private set; }

        private readonly Action<float> _onApply;

        public SliderOptionItem( string label, float minValue, float maxValue, float initialValue, Action<float> onApply )
        {
            Label = label;
            MinValue = minValue;
            MaxValue = maxValue;
            CurrentValue = initialValue;
            _onApply = onApply;
        }

        /// <summary>
        /// 値の変更を確定し、適用処理を呼び出します
        /// </summary>
        public void Apply( float value )
        {
            CurrentValue = value;
            _onApply?.Invoke( value );
        }
    }
}
