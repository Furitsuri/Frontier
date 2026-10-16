using TMPro;
using UnityEngine;

namespace Frontier.UI
{
    /// <summary>
    /// グリッドカーソルが選択しているタイルの高さを表示するUIです。
    /// 表示する文字列・表示するか否かの判断はPresenter側で行い、このクラスは指示を適用するのみとします。
    /// </summary>
    public class TileHeightUI : UiMonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _heightText;

        /// <summary>
        /// 表示する文字列を設定します
        /// </summary>
        public void SetHeightText( string text )
        {
            _heightText.text = text;
        }

        /// <summary>
        /// 表示・非表示を切り替えます
        /// </summary>
        public void SetVisible( bool isVisible )
        {
            if( gameObject.activeSelf == isVisible ) { return; }

            gameObject.SetActive( isVisible );
        }
    }
}
