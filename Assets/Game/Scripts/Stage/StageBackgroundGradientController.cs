using Frontier.Registries;
using UnityEngine;
using Zenject;

namespace Frontier.Stage
{
    /// <summary>
    /// 戦闘ステージの背景(Skybox)に、上段・中段・下段の3色グラデーションを適用します。
    /// 現時点ではステージデータが背景色に対応していないため、固定の既定配色を使用します。
    /// 将来的にステージデータへ配色情報を追加する際は、StageController.Init() のステージ読込直後で
    /// ApplyGradient() を呼び出す想定です。
    /// </summary>
    public class StageBackgroundGradientController
    {
        // ステージデータが背景色に対応するまでの暫定既定配色
        private static readonly Color DefaultTopColor    = Color.white;
        private static readonly Color DefaultMiddleColor = new Color( 0.29f, 0.56f, 0.85f );
        private static readonly Color DefaultBottomColor = Color.black;

        private static readonly int TopColorId    = Shader.PropertyToID( "_TopColor" );
        private static readonly int MiddleColorId = Shader.PropertyToID( "_MiddleColor" );
        private static readonly int BottomColorId = Shader.PropertyToID( "_BottomColor" );

        [Inject] private PrefabRegistry _prefabReg = null;

        private Material _skyboxMaterialInstance = null;

        /// <summary>
        /// 元のマテリアルアセットを書き換えないよう、インスタンス化しておきます。
        /// </summary>
        public void Setup()
        {
            _skyboxMaterialInstance = Object.Instantiate( _prefabReg.StageBackgroundGradientMaterial );
        }

        /// <summary>上段・中段・下段の色を指定してグラデーションを適用します。</summary>
        public void ApplyGradient( in Color topColor, in Color middleColor, in Color bottomColor )
        {
            _skyboxMaterialInstance.SetColor( TopColorId,    topColor );
            _skyboxMaterialInstance.SetColor( MiddleColorId, middleColor );
            _skyboxMaterialInstance.SetColor( BottomColorId, bottomColor );

            RenderSettings.skybox = _skyboxMaterialInstance;
        }

        /// <summary>ステージデータが背景色に対応するまでの暫定処理として、固定の既定配色を適用します。</summary>
        public void ApplyDefaultGradient() => ApplyGradient( DefaultTopColor, DefaultMiddleColor, DefaultBottomColor );
    }
}
