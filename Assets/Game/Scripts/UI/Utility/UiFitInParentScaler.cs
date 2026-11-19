using UnityEngine;

namespace Frontier.UI
{
    /// <summary>
    /// 固定の設計サイズでレイアウトしたUIのまとまりを、縦横比を保ったまま親の矩形(余白を除く)に収まるよう拡大縮小します。
    /// CanvasScalerは画面の横幅に合わせて拡縮するため、画面が横長になるとキャンバスの縦幅が設計時より短くなり、
    /// 中央基準で配置したUIが画面外にはみ出したり、画面下端の入力ガイド等と重なったりします。それを防ぐために使用します。
    /// 子のUIはこのオブジェクトの中心を基準(anchor:center)に、設計サイズの範囲内へ配置してください。
    /// </summary>
    [ExecuteAlways]
    [RequireComponent( typeof( RectTransform ) )]
    public class UiFitInParentScaler : MonoBehaviour
    {
        [Header( "設計サイズ(子のUIを配置する範囲)" )]
        [SerializeField] private Vector2 _designSize = new Vector2( 1000f, 640f );

        [Header( "親の矩形の上下左右から除く余白(ヘッダー・入力ガイド等の領域)" )]
        [SerializeField] private float _paddingTop      = 0f;
        [SerializeField] private float _paddingBottom   = 0f;
        [SerializeField] private float _paddingLeft     = 0f;
        [SerializeField] private float _paddingRight    = 0f;

        [Header( "余白を除いた範囲が設計サイズより大きい場合に拡大するか" )]
        [SerializeField] private bool _allowUpscale = false;

        private RectTransform _rectTransform = null;

        private void OnEnable()
        {
            Fit();
        }

        private void LateUpdate()
        {
            Fit();
        }

        /// <summary>
        /// 親の矩形に合わせて、位置と拡大率を更新します。
        /// </summary>
        public void Fit()
        {
            if( _rectTransform == null ) { _rectTransform = ( RectTransform ) transform; }

            var parent = _rectTransform.parent as RectTransform;
            if( parent == null || _designSize.x <= 0f || _designSize.y <= 0f ) { return; }

            var parentSize      = parent.rect.size;
            float availableW    = Mathf.Max( 0f, parentSize.x - _paddingLeft - _paddingRight );
            float availableH    = Mathf.Max( 0f, parentSize.y - _paddingTop - _paddingBottom );
            float scale         = Mathf.Min( availableW / _designSize.x, availableH / _designSize.y );
            if( !_allowUpscale ) { scale = Mathf.Min( scale, 1f ); }

            // 親の中心を基準に、余白を除いた範囲の中央へ配置する
            _rectTransform.anchorMin        = new Vector2( 0.5f, 0.5f );
            _rectTransform.anchorMax        = new Vector2( 0.5f, 0.5f );
            _rectTransform.pivot            = new Vector2( 0.5f, 0.5f );
            _rectTransform.sizeDelta        = _designSize;
            _rectTransform.anchoredPosition = new Vector2( ( _paddingLeft - _paddingRight ) * 0.5f, ( _paddingBottom - _paddingTop ) * 0.5f );
            _rectTransform.localScale       = new Vector3( scale, scale, 1f );
        }
    }
}
