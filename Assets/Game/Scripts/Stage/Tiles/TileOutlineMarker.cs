using UnityEngine;

namespace Frontier.Stage
{
    /// <summary>
    /// 特定のタイルの位置を外枠で示すための汎用マーカーです。
    /// 見た目(外枠の形状・色・太さ・マテリアル・タイル表面からの高さ等)及びアニメーション演出はすべて
    /// プレハブ(TileOutlineMarker.prefab)とそのAnimatorController側で設定し、
    /// このクラスはタイル位置への配置と表示・非表示の切り替えのみを担います。
    /// 生成はPrefabRegistry.TileOutlineMarkerPrefabを基に、HierarchyBuilderBase経由で行ってください。
    /// </summary>
    public class TileOutlineMarker : MonoBehaviour
    {
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
    }
}
