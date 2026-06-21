using UnityEngine;

namespace Frontier.Field
{
    /// <summary>
    /// フィールドのレイヤー(Free レイヤー)生成パラメータ。Inspector で調整する。
    /// ノードの Layer がファイルで明示された Fixed レイヤーは生成対象外（テンプレートの値をそのまま使用）。
    /// </summary>
    [CreateAssetMenu( fileName = "FieldGenerationConfig", menuName = "Frontier/Field/FieldGenerationConfig" )]
    public class FieldGenerationConfig : ScriptableObject
    {
        [Header( "Bossのレイヤー番号の範囲(Bossノードでファイル側にLayerが未指定の場合のみ使用)" )]
        [SerializeField] private Vector2Int _layerCountRange = new Vector2Int( 3, 5 );

        [Header( "1レイヤーあたりのノード数" )]
        [SerializeField] private Vector2Int _nodesPerLayerRange = new Vector2Int( 1, 3 );

        [Header( "1ノードから次レイヤーへの分岐数" )]
        [SerializeField] private Vector2Int _branchCountRange = new Vector2Int( 1, 2 );

        [Header( "中間ノードタイプの出現率(Start/Bossを除く)" )]
        [SerializeField]
        private FieldNodeTypeWeight[] _typeWeights = new[]
        {
            new FieldNodeTypeWeight { Type = FieldNodeType.Battle,  Weight = 0.6f },
            new FieldNodeTypeWeight { Type = FieldNodeType.Recruit, Weight = 0.2f },
            new FieldNodeTypeWeight { Type = FieldNodeType.Rest,    Weight = 0.2f },
        };

        [Header( "Battleノードに割り当てるStageIndexの範囲(両端含む、FilePathRegistry.StageNames[]のインデックス)" )]
        [SerializeField] private Vector2Int _stageIndexRange = new Vector2Int( 0, 0 );

        [Header( "レイアウト: レイヤー間のX間隔 / レイヤー内のY間隔" )]
        [SerializeField] private float _layerSpacingX = 4f;
        [SerializeField] private float _nodeSpacingY  = 3f;

        public Vector2Int             LayerCountRange     => _layerCountRange;
        public Vector2Int             NodesPerLayerRange  => _nodesPerLayerRange;
        public Vector2Int             BranchCountRange    => _branchCountRange;
        public FieldNodeTypeWeight[]  TypeWeights         => _typeWeights;
        public Vector2Int             StageIndexRange     => _stageIndexRange;
        public float                  LayerSpacingX       => _layerSpacingX;
        public float                  NodeSpacingY        => _nodeSpacingY;
    }
}
