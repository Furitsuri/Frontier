using UnityEngine;

namespace Frontier.Stage
{
    /// <summary>
    /// 特定のタイルを目立たせるための汎用マーカーです。
    /// 見た目(枠の形状・色・太さ・マテリアル等)はプレハブ(TileHighlightMarker.prefab)側で設定し、
    /// このクラスはタイル位置への配置と表示・非表示の切り替えのみを担います。
    /// 生成はPrefabRegistry.TileHighlightMarkerPrefabを基に、HierarchyBuilderBase経由で行ってください。
    /// </summary>
    public class TileHighlightMarker : MonoBehaviour
    {
        [Tooltip( "タイル表面からの表示高さ。移動可能範囲等のタイルメッシュ(ADD_TILE_POS_Y刻みで重なる)に埋もれない値を設定してください" )]
        [SerializeField] private float _heightOffset = 0.06f;

        /// <summary>
        /// 指定したタイル表面の位置にマーカーを表示します
        /// </summary>
        /// <param name="tileSurfacePos">対象タイルの表面座標(TileStaticData.CursorStandPos)</param>
        public void Show( Vector3 tileSurfacePos )
        {
            transform.position = tileSurfacePos + Vector3.up * _heightOffset;
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
