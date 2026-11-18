using TMPro;
using UnityEngine;

namespace Frontier.UI
{
    public class TooltipUI : UiMonoBehaviour
    {
        [SerializeField] TextMeshProUGUI _explanation;

        RectTransform _rectTransform = null;
        float _minHeight = 0f;  // ウィンドウの縦幅の最小値(Prefabで設定された縦幅)

        /// <summary>
        /// 説明文を設定します。説明文が収まるよう、ウィンドウの縦幅を伸ばします(Prefabで設定された縦幅より短くはしません)。
        /// MEMO : ウィンドウのpivotは上端のため、縦幅は下方向へ伸びます。
        /// </summary>
        public void SetText( string text )
        {
            _explanation.text = text;

            // 説明文はウィンドウに対して上下左右の余白を持って伸縮する(anchor:stretch)ため、その余白分を加えた縦幅にする
            var explanationRect = _explanation.rectTransform;
            float textWidth     = _rectTransform.sizeDelta.x + explanationRect.sizeDelta.x;
            float verticalSpace = -explanationRect.sizeDelta.y;
            float textHeight    = _explanation.GetPreferredValues( text, textWidth, 0f ).y;

            var size = _rectTransform.sizeDelta;
            size.y = Mathf.Max( _minHeight, textHeight + verticalSpace );
            _rectTransform.sizeDelta = size;
        }

        public void SetPosition( Vector2 pos )
        {
            _rectTransform.anchoredPosition = pos;
        }

        public Vector2 GetSize()
        {
            return _rectTransform.sizeDelta;
        }

        public Vector2 GetPivot()
        {
            return _rectTransform.pivot;
        }

        public override void Setup()
        {
            _rectTransform = GetComponent<RectTransform>();
            if( _minHeight <= 0f ) { _minHeight = _rectTransform.sizeDelta.y; }

            gameObject.SetActive( false );
        }
    }
}
