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

        /// <summary>
        /// ウィンドウを、親のローカル座標で指定した矩形の位置に配置します(大きさは変更しません)。
        /// </summary>
        public void SetLocalRect( Rect rect )
        {
            var pivot = _rectTransform.pivot;
            var pos   = new Vector2( rect.xMin + rect.width * pivot.x, rect.yMin + rect.height * pivot.y );

            _rectTransform.localPosition = new Vector3( pos.x, pos.y, _rectTransform.localPosition.z );
        }

        /// <summary>
        /// 指定のRectTransformの矩形を、このウィンドウの親のローカル座標に変換して返します。
        /// 項目・画面・入力ガイド等の位置を、ウィンドウの配置と同じ座標系で扱うために使用します。
        /// </summary>
        public Rect ToLocalRect( RectTransform target )
        {
            var corners = new Vector3[4];
            target.GetWorldCorners( corners );

            var parent  = _rectTransform.parent;
            Vector2 min = parent.InverseTransformPoint( corners[0] );
            Vector2 max = parent.InverseTransformPoint( corners[2] );

            return Rect.MinMaxRect( Mathf.Min( min.x, max.x ), Mathf.Min( min.y, max.y ), Mathf.Max( min.x, max.x ), Mathf.Max( min.y, max.y ) );
        }

        public Vector2 GetSize()
        {
            return _rectTransform.sizeDelta;
        }

        public override void Setup()
        {
            _rectTransform = GetComponent<RectTransform>();
            if( _minHeight <= 0f ) { _minHeight = _rectTransform.sizeDelta.y; }

            gameObject.SetActive( false );
        }
    }
}
