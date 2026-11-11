using System;

namespace Frontier.Option
{
    /// <summary>
    /// 複数の選択肢から1つを選ぶオプション項目(早送り倍率など)。
    /// 値は選択肢のインデックスで扱います。
    /// </summary>
    public class ChoiceOptionItem : IOptionItem
    {
        public string Label { get; }
        public string[] Choices { get; }
        public int CurrentIndex { get; private set; }

        private readonly Action<int> _onApply;

        public ChoiceOptionItem( string label, string[] choices, int initialIndex, Action<int> onApply )
        {
            Label           = label;
            Choices         = choices;
            CurrentIndex    = initialIndex;
            _onApply        = onApply;
        }

        /// <summary>
        /// 値の変更を確定し、適用処理を呼び出します
        /// </summary>
        public void Apply( int index )
        {
            CurrentIndex = index;
            _onApply?.Invoke( index );
        }
    }
}
