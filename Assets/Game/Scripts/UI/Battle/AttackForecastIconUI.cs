using UnityEngine;
using UnityEngine.UI;

namespace Frontier.UI
{
    /// <summary>
    /// 攻撃予告線を伸ばしているキャラクターが画面から見切れている場合に、そのキャラクターが誰なのかを示すアイコン1つ分です。
    /// 見た目(枠の形状・色・太さ、枠内の背景、画像の収め方、大きさ)はすべてプレハブ(AttackForecastIcon.prefab)側で設定し、
    /// このクラスは渡された画像のはめ込みと、指示された画面上の位置への配置、表示・非表示の切り替えのみを担います。
    /// 生成はBattleUISystem.CreateAttackForecastIconで行ってください。
    /// </summary>
    public class AttackForecastIconUI : MonoBehaviour
    {
        [SerializeField] private RawImage _portraitImage;           // 枠内に収めるキャラクターの画像
        [SerializeField] private float _screenEdgeMargin = 60f;     // 画面端からアイコンの中心までの最小距離(キャンバス上の長さ)

        private RectTransform _btlUiRectTransform;
        private RectTransform _selfRectTransform;
        private Camera _btlUiCamera;
        private Canvas _canvas;

        /// <summary>
        /// 画面端からアイコンの中心までの最小距離を、画面上のピクセル数で取得します
        /// </summary>
        public float ScreenEdgeMarginPixels => _screenEdgeMargin * ( null != _canvas ? _canvas.scaleFactor : 1f );

        /// <summary>
        /// 初期化します
        /// </summary>
        /// <param name="rect">BattleUISystemのRectTransform</param>
        /// <param name="uiCamera">BattleUISystemに用いるUI用カメラ</param>
        public void Init( RectTransform rect, Camera uiCamera )
        {
            _btlUiRectTransform = rect;
            _btlUiCamera        = uiCamera;
            _selfRectTransform  = GetComponent<RectTransform>();
            _canvas             = rect.GetComponentInParent<Canvas>();
        }

        /// <summary>
        /// 枠内に収めるキャラクターの画像を設定します
        /// </summary>
        public void SetPortrait( Texture portrait )
        {
            _portraitImage.texture = portrait;
        }

        /// <summary>
        /// 指定した画面上の位置にアイコンを表示します
        /// </summary>
        /// <param name="screenPos">画面上の位置(ピクセル)</param>
        public void Show( Vector2 screenPos )
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle( _btlUiRectTransform, screenPos, _btlUiCamera, out var pos );
            _selfRectTransform.localPosition = pos;

            if( !gameObject.activeSelf ) { gameObject.SetActive( true ); }
        }

        /// <summary>
        /// アイコンを非表示にします
        /// </summary>
        public void Hide()
        {
            if( gameObject.activeSelf ) { gameObject.SetActive( false ); }
        }
    }
}
