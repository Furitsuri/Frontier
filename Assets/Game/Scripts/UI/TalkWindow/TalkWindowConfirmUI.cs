using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

        [Header( "ConfirmUIType.SubButtons時のみ表示する、はい/いいえの横のSUB1/SUB2ガイドアイコン\n" +
                 "(0:はいの左、1:いいえの右)" )]
        [SerializeField] private Image[] _optionIcons;

        // Yes/No行の分だけ、通常の会話ウィンドウより高さを追加で確保する(実測値)
        private const float ConfirmOptionsExtraHeight = 40f;

        // 選択肢テキストの左右端からの余白(px)。アイコン表示時はアイコン+間隔の分だけ内側へ寄せる(実測値)
        private const float OptionTextInsetWithoutIcon = 30f;
        private const float OptionTextInsetWithIcon    = 70f;

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
        /// ConfirmUIType.SubButtons時に表示するSUB1/SUB2アイコンの表示/非表示を切り替えます。
        /// アイコン表示時は選択肢テキストをアイコン分だけ内側へ寄せ、非表示時は元の余白に戻します。
        /// </summary>
        public void SetOptionIconsVisible( bool isVisible )
        {
            foreach ( var icon in _optionIcons ) { icon.gameObject.SetActive( isVisible ); }

            float inset = isVisible ? OptionTextInsetWithIcon : OptionTextInsetWithoutIcon;
            var yesRect = ( RectTransform ) _optionTexts[0].transform;
            var noRect  = ( RectTransform ) _optionTexts[1].transform;
            yesRect.anchoredPosition = new Vector2( inset, yesRect.anchoredPosition.y );
            noRect.anchoredPosition  = new Vector2( -inset, noRect.anchoredPosition.y );
        }

        /// <summary>
        /// SUB1/SUB2アイコンのスプライトを設定します。
        /// </summary>
        public void SetOptionIcons( Sprite yesIcon, Sprite noIcon )
        {
            _optionIcons[0].sprite = yesIcon;
            _optionIcons[1].sprite = noIcon;
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
