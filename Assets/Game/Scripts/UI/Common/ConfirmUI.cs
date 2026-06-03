using TMPro;
using UnityEngine;

namespace Frontier
{
    public class ConfirmUI : UiMonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private TextMeshProUGUI[] _confirmTMPTexts;

        [Header( "ウィンドウ幅自動調整" )]
        [SerializeField] private float _horizontalPadding = 40f;
        [SerializeField] private float _minWidth          = 200f;

        public void Init()
        {
            SetActiveAlternativeText( true );
        }

        /// <summary>
        /// メッセージを設定し、テキストが収まるようにウィンドウ幅を自動調整します。
        /// </summary>
        public void SetMessageText( string message )
        {
            _messageText.text = message;
            AdjustWindowWidth();
        }

        public void SetActiveAlternativeText( bool isActive )
        {
            foreach( var tmpText in _confirmTMPTexts )
            {
                tmpText.gameObject.SetActive( isActive );
            }
        }

        /// <summary>
        /// 選択しているインデックスに該当する文字色を変更します
        /// </summary>
        /// <param name="selectIndex">選択中のインデックス値</param>
        public void ApplyTextColor( int selectIndex )
        {
            for( int i = 0; i < _confirmTMPTexts.Length; ++i )
            {
                _confirmTMPTexts[i].color = ( i == selectIndex ) ? Color.yellow : Color.gray;
            }
        }

        /// <summary>
        /// メッセージテキストの最長行幅をもとにウィンドウ幅を調整します。
        /// _windowRect が未設定の場合は何もしません。
        /// </summary>
        private void AdjustWindowWidth()
        {
            var windowRect = GetComponent<RectTransform>();
            if( null == windowRect ) { return; }

            // 制約なし（幅・高さとも無制限）で最長行の表示幅を取得
            float textWidth  = _messageText.GetPreferredValues( _messageText.text, float.PositiveInfinity, float.PositiveInfinity ).x;
            var   sizeDelta  = windowRect.sizeDelta;
            sizeDelta.x      = Mathf.Max( textWidth + _horizontalPadding, _minWidth );
            windowRect.sizeDelta = sizeDelta;
        }
    }
}
