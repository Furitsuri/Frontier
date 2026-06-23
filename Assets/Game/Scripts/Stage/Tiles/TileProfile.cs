using UnityEngine;

namespace Frontier.Stage
{
    /// <summary>
    /// タイルタイプごとの「見た目・挙動の差分」をまとめて保持するデータ。
    /// 水のように特殊な仕様を持つタイルは、専用クラスを派生させるのではなく
    /// このプロファイルに値を1つ追加することで表現します（データ駆動）。
    /// 生成は <see cref="TileMaterialLibrary"/> が一括で行います。
    /// </summary>
    public sealed class TileProfile
    {
        /// <summary>このタイプに使用するマテリアル</summary>
        public Material Material { get; }

        /// <summary>
        /// 見た目の「上面だけ」を下げる量（底面は他タイルと揃えたまま背を低くする）。
        /// 水を通常タイルより少しだけ低く見せるために使用します（0=下げない）。
        /// あくまで描画上の厚み調整で、移動・経路探索が参照する論理高さ
        /// (<see cref="TileStaticData.Height"/>) には影響しません。
        /// </summary>
        public float VisualHeightOffset { get; }

        /// <summary>
        /// 同じタイプの隣接タイルと接する側面を非表示にするか
        /// （水同士の仕切りを消してシームレスにするマインクラフト風の面カリング）。
        /// </summary>
        public bool UseSideFaceCulling { get; }

        /// <summary>
        /// このタイル上に立つキャラクターの立ち位置 Y に加算する補正値。
        /// 負の値にするとキャラクターが沈み、水に浸かっている表現などに使えます（0=補正なし）。
        /// </summary>
        public float CharaStandHeightOffset { get; }

        public TileProfile( Material material, float visualHeightOffset, bool useSideFaceCulling, float charaStandHeightOffset )
        {
            Material                = material;
            VisualHeightOffset      = visualHeightOffset;
            UseSideFaceCulling      = useSideFaceCulling;
            CharaStandHeightOffset  = charaStandHeightOffset;
        }
    }
}
