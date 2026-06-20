using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Frontier.Field
{
    /// <summary>
    /// テンプレートの Start/Boss ノードを固定したまま、その間の中間ノード(個数・種別・接続)を
    /// シード値に基づいて手続き的に生成する。同じシードを渡せば同じ結果になる。
    /// </summary>
    public static class FieldGenerator
    {
        public static FieldData Generate( FieldData template, FieldGenerationConfig config, int seed )
        {
            var startNode = CloneNode( FindNode( template, template.StartNodeId ) );
            var bossNode  = CloneNode( FindNode( template, template.BossNodeId ) );

            if ( startNode == null || bossNode == null )
            {
                Debug.LogError( "[FieldGenerator] テンプレートに Start または Boss ノードが見つかりません。" );
                return template;
            }

            var rng    = new System.Random( seed );
            int nextId = template.Nodes.Max( n => n.Id ) + 1;

            int layerCount = RandomRange( rng, config.LayerCountRange );

            var layers = new List<List<FieldNodeData>>( layerCount );
            for ( int layer = 0; layer < layerCount; layer++ )
            {
                int nodeCount = Mathf.Max( 1, RandomRange( rng, config.NodesPerLayerRange ) );
                var layerNodes = new List<FieldNodeData>( nodeCount );

                for ( int i = 0; i < nodeCount; i++ )
                {
                    var type = PickWeightedType( rng, config.TypeWeights );

                    layerNodes.Add( new FieldNodeData
                    {
                        Id         = nextId++,
                        Type       = ( int ) type,
                        StageIndex = type == FieldNodeType.Battle ? RandomRange( rng, config.StageIndexRange ) : -1,
                        PosX       = startNode.PosX + ( layer + 1 ) * config.LayerSpacingX,
                        PosY       = ( i - ( nodeCount - 1 ) * 0.5f ) * config.NodeSpacingY,
                        NextIds    = Array.Empty<int>(),
                        PathToNext = Array.Empty<FieldNodePath>(),
                    } );
                }

                layers.Add( layerNodes );
            }

            var allNodes      = new List<FieldNodeData> { startNode };
            var previousLayer = new List<FieldNodeData> { startNode };

            foreach ( var layer in layers )
            {
                ConnectLayers( rng, previousLayer, layer, config.BranchCountRange );
                allNodes.AddRange( layer );
                previousLayer = layer;
            }

            // 最終レイヤーから Boss へは全ノードを必ず接続する
            ConnectLayers( rng, previousLayer, new List<FieldNodeData> { bossNode }, new Vector2Int( 1, 1 ) );
            allNodes.Add( bossNode );

            return new FieldData
            {
                FieldId     = template.FieldId,
                Nodes       = allNodes.ToArray(),
                StartNodeId = template.StartNodeId,
                BossNodeId  = template.BossNodeId,
            };
        }

        private static void ConnectLayers( System.Random rng, List<FieldNodeData> from, List<FieldNodeData> to, Vector2Int branchRange )
        {
            var incoming = new HashSet<int>();

            foreach ( var f in from )
            {
                int branchCount = Mathf.Clamp( RandomRange( rng, branchRange ), 1, to.Count );
                var targets = to.OrderBy( _ => rng.Next() ).Take( branchCount ).ToList();

                f.NextIds = targets.Select( t => t.Id ).ToArray();
                foreach ( var t in targets ) incoming.Add( t.Id );
            }

            // 誰からも接続されていないノードがあれば、ランダムな from ノードから接続を追加する
            foreach ( var t in to )
            {
                if ( incoming.Contains( t.Id ) ) continue;

                var f = from[ rng.Next( from.Count ) ];
                f.NextIds = f.NextIds.Append( t.Id ).ToArray();
            }
        }

        private static FieldNodeType PickWeightedType( System.Random rng, FieldNodeTypeWeight[] weights )
        {
            float total = weights.Sum( w => w.Weight );
            float roll  = ( float ) rng.NextDouble() * total;
            float cumulative = 0f;

            foreach ( var w in weights )
            {
                cumulative += w.Weight;
                if ( roll <= cumulative ) return w.Type;
            }

            return weights[ weights.Length - 1 ].Type;
        }

        private static int RandomRange( System.Random rng, Vector2Int range )
        {
            int min = Mathf.Min( range.x, range.y );
            int max = Mathf.Max( range.x, range.y );
            return rng.Next( min, max + 1 );
        }

        private static FieldNodeData FindNode( FieldData data, int id )
        {
            foreach ( var node in data.Nodes )
            {
                if ( node.Id == id ) return node;
            }
            return null;
        }

        private static FieldNodeData CloneNode( FieldNodeData source )
        {
            if ( source == null ) return null;

            return new FieldNodeData
            {
                Id         = source.Id,
                Type       = source.Type,
                StageIndex = source.StageIndex,
                PosX       = source.PosX,
                PosY       = source.PosY,
                NextIds    = Array.Empty<int>(),
                PathToNext = Array.Empty<FieldNodePath>(),
            };
        }
    }
}
