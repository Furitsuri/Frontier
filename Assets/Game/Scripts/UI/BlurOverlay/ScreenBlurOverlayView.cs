using UnityEngine;
using UnityEngine.UI;

namespace Frontier.UI
{
    /// <summary>
    /// 画面全体を「ぼかし+単色の覆い」で覆う汎用オーバーレイのViewです。
    /// 自身のCanvas(overrideSorting)より手前に描画されるUI(ヘッダー・会話ウィンドウ等の常設UI)はそのまま見え、
    /// 奥に描画されるもの(各画面のUI・3D背景)だけがぼかされて暗く覆われます。
    /// そのため、ぼかしたい画面のUIのCanvas(既定5)と、常設UI(GeneralUI、10)の間のsortingOrderに置きます。
    /// この上に、覆いから浮かび上がらせたい表示(確認内容等)を出したい場合は、その表示をさらに手前の
    /// sortingOrderを持つネストしたCanvasに置いてください。
    /// 背景のぼかしにはUIBlurBackgroundシェーダー(GrabPass)を使うため、Built-inレンダーパイプライン、かつ
    /// Canvasの描画モードがScreen Space - Cameraである必要があります(全シーンのGeneralUIは該当)。
    /// 表示中は毎フレーム画面全体のコピーを2回行うため、必要な間だけ表示してください。
    /// 各StateはIUiSystemに直接アクセスせず、ScreenBlurOverlayPresenter経由で表示・非表示を行ってください。
    /// </summary>
    public class ScreenBlurOverlayView : UiMonoBehaviour
    {
        private const string BLUR_SIZE_PROPERTY = "_BlurSize";

        [Header( "覆いの画像(UIBlurBackgroundシェーダーのマテリアルを設定したImage。このGameObject自身に付ける)" )]
        [SerializeField] private Image _coverImage;

        [Header( "既定の見た目" )]
        [SerializeField] private float _defaultBlurSize = 1.5f;
        [SerializeField] private Color _defaultCoverColor = new Color( 0.3f, 0.3f, 0.3f, 0.6f );

        // 表示のたびに見た目を変えられるよう、共有マテリアル(アセット)を書き換えないインスタンスを使う
        private Material _runtimeMaterial = null;

        public override void Setup()
        {
            base.Setup();

            if ( _coverImage == null || _runtimeMaterial != null ) { return; }

            _runtimeMaterial     = new Material( _coverImage.material );
            _coverImage.material = _runtimeMaterial;
        }

        private void OnDestroy()
        {
            if ( _runtimeMaterial != null ) { Destroy( _runtimeMaterial ); }
        }

        /// <summary>
        /// 既定の見た目で画面全体を覆います。
        /// </summary>
        public void Show() => Show( _defaultBlurSize, _defaultCoverColor );

        /// <summary>
        /// 画面全体を覆います。
        /// </summary>
        /// <param name="blurSize">ぼかしの強さ(1080p換算でのタップ間隔[px]。ぼかし半径はその約8倍。0でぼかし無しの覆いのみ)</param>
        /// <param name="coverColor">覆いの色(rgb=色、a=覆いの濃さ。aが1で背景は完全に隠れる)</param>
        public void Show( float blurSize, Color coverColor )
        {
            if ( _runtimeMaterial != null ) { _runtimeMaterial.SetFloat( BLUR_SIZE_PROPERTY, blurSize ); }
            if ( _coverImage != null )      { _coverImage.color = coverColor; }

            gameObject.SetActive( true );
        }

        public void Hide() => gameObject.SetActive( false );
    }
}
