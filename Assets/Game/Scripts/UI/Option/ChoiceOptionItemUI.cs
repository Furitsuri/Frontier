using System;
using TMPro;
using UnityEngine;

namespace Frontier.UI
{
    /// <summary>
    /// 複数の選択肢から1つを選ぶ形式のオプション項目View。
    /// 選択中の選択肢の文字列を表示し、左右入力(または決定入力)で選択肢を切り替えます。
    /// </summary>
    public class ChoiceOptionItemUI : OptionItemUIBase
    {
        [SerializeField] private TextMeshProUGUI _valueText;

        private string[] _choices = Array.Empty<string>();
        private int _index = 0;

        public event Action<int> OnValueChanged;

        public void SetChoices( string[] choices )
        {
            _choices = choices ?? Array.Empty<string>();
        }

        public void SetIndexWithoutNotify( int index )
        {
            if( _choices.Length <= 0 ) { return; }

            _index = Mathf.Clamp( index, 0, _choices.Length - 1 );
            _valueText.text = _choices[_index];
        }

        /// <summary>
        /// 選択肢をdelta分ずらします(末尾・先頭で循環します)。キーボード・パッドからの操作に用います
        /// </summary>
        public void Step( int delta )
        {
            if( _choices.Length <= 0 ) { return; }

            int count = _choices.Length;
            SetIndexWithoutNotify( ( ( _index + delta ) % count + count ) % count );
            OnValueChanged?.Invoke( _index );
        }
    }
}
