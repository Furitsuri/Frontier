using TMPro;
using UnityEngine;

namespace Frontier.UI
{
    /// <summary>
    /// TalkWindowUIを拡張し、ウィンドウ内に「はい/いいえ」の二択を表示できるようにしたView。
    /// 会話ウィンドウの見た目のまま、雇用/解雇の完了確認等、Yes/No選択を要する会話に使う。
    /// カーソル位置の判定・入力受付はConfirmPhaseStateBase側が持つため、このクラスは
    /// 表示指示(テキスト・色)を受けて反映するだけに留める。
    /// </summary>
    public class TalkWindowConfirmUI : TalkWindowUI
    {
        [Header( "はい/いいえの選択肢テキスト(0:はい、1:いいえ)" )]
        [SerializeField] private TextMeshProUGUI[] _optionTexts;

        // Yes/No行の分だけ、通常の会話ウィンドウより高さを追加で確保する(実測値)
        private const float ConfirmOptionsExtraHeight = 40f;

        protected override float ReservedExtraHeight => ConfirmOptionsExtraHeight;

        /// <summary>
        /// 「はい」「いいえ」に表示する文字列を設定します。
        /// </summary>
        public void SetOptionTexts( string yesText, string noText )
        {
            _optionTexts[0].text = yesText;
            _optionTexts[1].text = noText;
        }

        /// <summary>
        /// 「はい」「いいえ」の表示/非表示を切り替えます。
        /// </summary>
        public void SetOptionsActive( bool isActive )
        {
            foreach ( var text in _optionTexts ) { text.gameObject.SetActive( isActive ); }
        }

        /// <summary>
        /// 選択中の項目(0:はい、1:いいえ)の文字色を強調し、他方は通常色に戻します。
        /// </summary>
        public void ApplyOptionColor( int selectedIndex )
        {
            for ( int i = 0; i < _optionTexts.Length; ++i )
            {
                _optionTexts[i].color = ( i == selectedIndex ) ? Color.yellow : Color.gray;
            }
        }
    }
}
