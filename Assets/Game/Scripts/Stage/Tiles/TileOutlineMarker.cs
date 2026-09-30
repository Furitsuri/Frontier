using System.Collections.Generic;
using UnityEngine;

namespace Frontier.Stage
{
    /// <summary>
    /// 特定のタイルの位置を外枠で示すための汎用マーカーです。
    /// 見た目(外枠の形状・既定の色・太さ・マテリアル・タイル表面からの高さ等)及びアニメーション演出はすべて
    /// プレハブ(TileOutlineMarker.prefab)とそのAnimatorController側で設定し、
    /// このクラスはタイル位置への配置、表示・非表示の切り替え、及び用途に応じた色の上書きのみを担います。
    /// 生成はPrefabRegistry.TileOutlineMarkerPrefabを基に、HierarchyBuilderBase経由で行ってください。
    /// </summary>
    public class TileOutlineMarker : MonoBehaviour
    {
        // 外枠毎のプレハブ上の元の色(SetColorで上書きした後もResetColorで戻せるよう、また外枠毎の濃さの違いを保つために保持する)
        private readonly Dictionary<LineRenderer, Color> _defaultColors = new Dictionary<LineRenderer, Color>();

        /// <summary>
        /// 指定したタイル表面の位置にマーカーを表示します
        /// (プレハブのルートがタイル表面に一致するよう配置し、高さ方向のずらしはプレハブ内の子オブジェクト側で行います)
        /// </summary>
        /// <param name="tileSurfacePos">対象タイルの表面座標(TileStaticData.CursorStandPos)</param>
        public void Show( Vector3 tileSurfacePos )
        {
            transform.position = tileSurfacePos;
            gameObject.SetActive( true );
        }

        /// <summary>
        /// マーカーを非表示にします
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive( false );
        }

        /// <summary>
        /// 外枠の色を指定色で上書きします。
        /// 不透明度はプレハブ上の各外枠の値に指定色のアルファを乗算するため、外枠毎の濃さの違い(演出)は保たれます。
        /// </summary>
        public void SetColor( Color color )
        {
            CacheDefaultColors();

            foreach( var entry in _defaultColors )
            {
                var applied = new Color( color.r, color.g, color.b, entry.Value.a * color.a );
                entry.Key.startColor = applied;
                entry.Key.endColor   = applied;
            }
        }

        /// <summary>
        /// 外枠の色をプレハブ上の既定の色に戻します
        /// </summary>
        public void ResetColor()
        {
            CacheDefaultColors();

            foreach( var entry in _defaultColors )
            {
                entry.Key.startColor = entry.Value;
                entry.Key.endColor   = entry.Value;
            }
        }

        /// <summary>
        /// 初回のみ、非アクティブなものも含めた全外枠のプレハブ上の色を保持します
        /// (非アクティブ状態で生成された場合Awakeが呼ばれないため、使用時に遅延して取得します)
        /// </summary>
        private void CacheDefaultColors()
        {
            if( 0 < _defaultColors.Count ) { return; }

            foreach( var lineRenderer in GetComponentsInChildren<LineRenderer>( true ) )
            {
                _defaultColors[lineRenderer] = lineRenderer.startColor;
            }
        }
    }
}
