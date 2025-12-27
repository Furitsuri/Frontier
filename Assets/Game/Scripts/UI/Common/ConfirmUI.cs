using TMPro;
using UnityEngine;

namespace Frontier
{
    public class ConfirmUI : UiMonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private TextMeshProUGUI[] _confirmTMPTexts;

        public void Init()
        {
            SetActiveAlternativeText( true );
        }

        public void SetMessageText( string message )
        {
            _messageText.text = message;
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
                if( i == selectIndex )
                {
                    _confirmTMPTexts[i].color = Color.yellow;
                }
                else
                {
                    _confirmTMPTexts[i].color = Color.gray;
                }
            }
        }
    }
}
