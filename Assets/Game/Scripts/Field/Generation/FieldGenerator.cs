using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Frontier.Field
{
    /// <summary>
    /// Start(レイヤー0)とBoss(ファイル指定または抽選で決まるレイヤー)の間を、レイヤー単位で生成する。
    ///
    /// レイヤーのうち、ファイル側で Layer が明示されたノードが1つでも存在するレイヤーは
    /// 「Fixedレイヤー」として扱われ、ランダム生成の対象から除外される。
    /// それ以外のレイヤーは「Freeレイヤー」として、Config の設定に基づきランダム生成される。
    ///
    /// 隣接レイヤー間の接続:
    ///   - 両方Fixedな隣接レイヤー間は、ファイル側で NextIds が完全に記述されていることを要求する
    ///     (未設定のノードがあればエラーとして中断する)。
    ///   - 片方でもFreeな隣接レイヤー間は、システムが自動で接続する
    ///     (Fixed側ノードの NextIds はこの場合上書きされる。まだ存在しないランダムノードを
    ///      ファイル側で先取りして指定することはできないため)。
    ///
    /// 同じシードを渡せば同じ結果になる。
    /// </summary>
    public static class FieldGenerator
    {
        public static FieldData Generate( FieldData template, FieldGenerationConfig config, int seed )
        {
            var nodesById = template.Nodes.ToDictionary( n => n.Id, CloneNodeFull );

            if ( !nodesById.TryGetValue( template.StartNodeId, out var startNode ) )
            {
                Debug.LogError( "[FieldGenerator] テンプレートに Start ノードが見つかりません。" );
                return template;
            }
            if ( !nodesById.TryGetValue( template.BossNodeId, out var bossNode ) )
            {
                Debug.LogError( "[FieldGenerator] テンプレートに Boss ノードが見つかりません。" );
                return template;
            }

            var rng = new System.Random( seed );

            // Start は常にレイヤー0。Boss のレイヤーはファイルで明示されていればそれを使い、
            // 未指定ならConfigの範囲からランダムに決定する
            int bossLayer = bossNode.Layer >= 0 ? bossNode.Layer : RandomRange( rng, config.LayerCountRange );
            if ( bossLayer < 1 )
            {
                Debug.LogError( $"[FieldGenerator] Bossレイヤー({bossLayer})が不正です。1以上である必要があります。" );
                return template;
            }

            // レイヤーごとに、ファイルで明示的に配置された(Layer >= 0 の)ノードを集計する
            var layerNodes = new Dictionary<int, List<FieldNodeData>> { [ 0 ] = new List<FieldNodeData> { startNode } };
            var fixedLayers = new HashSet<int> { 0, bossLayer };

            foreach ( var node in nodesById.Values )
            {
                if ( node.Id == template.StartNodeId || node.Id == template.BossNodeId ) continue;
                if ( node.Layer < 0 ) continue; // 未指定ノードはレイヤー構造には参加しない(従来データとの互換のため出力には残す)

                if ( !layerNodes.TryGetValue( node.Layer, out var list ) )
                {
                    list = new List<FieldNodeData>();
                    layerNodes[ node.Layer ] = list;
                }
                list.Add( node );
                fixedLayers.Add( node.Layer );
            }

            if ( !layerNodes.TryGetValue( bossLayer, out var bossLayerList ) )
            {
                bossLayerList = new List<FieldNodeData>();
                layerNodes[ bossLayer ] = bossLayerList;
            }
            bossLayerList.Add( bossNode );

            // レイヤー1 ~ bossLayer-1 のうち、Fixedでない(Free)レイヤーをランダム生成する
            int nextId = template.Nodes.Max( n => n.Id ) + 1;
            var generatedNodes = new List<FieldNodeData>();

            for ( int layer = 1; layer < bossLayer; layer++ )
            {
                if ( fixedLayers.Contains( layer ) ) continue;

                int nodeCount = Mathf.Max( 1, RandomRange( rng, config.NodesPerLayerRange ) );
                var nodes = new List<FieldNodeData>( nodeCount );

                for ( int i = 0; i < nodeCount; i++ )
                {
                    var type = PickWeightedType( rng, config.TypeWeights );
                    nodes.Add( new FieldNodeData
                    {
                        Id         = nextId++,
                        Type       = ( int ) type,
                        Layer      = layer,
                        StageIndex = type == FieldNodeType.Battle ? RandomRange( rng, config.StageIndexRange ) : -1,
                        PosX       = startNode.PosX + layer * config.LayerSpacingX,
                        PosY       = ( i - ( nodeCount - 1 ) * 0.5f ) * config.NodeSpacingY,
                        NextIds    = Array.Empty<int>(),
                        PathToNext = Array.Empty<FieldNodePath>(),
                    } );
                }

                layerNodes[ layer ] = nodes;
                generatedNodes.AddRange( nodes );
            }

            // 隣接レイヤー間の接続を決定する
            for ( int layer = 0; layer < bossLayer; layer++ )
            {
                var from = layerNodes[ layer ];
                var to   = layerNodes[ layer + 1 ];
                bool fromIsFixed = fixedLayers.Contains( layer );
                bool toIsFixed   = fixedLayers.Contains( layer + 1 );

                if ( fromIsFixed && toIsFixed )
                {
                    // 両方Fixed: ファイル側で接続が完全に記述されていることを要求する
                    foreach ( var f in from )
                    {
                        if ( f.NextIds == null || f.NextIds.Length == 0 )
                        {
                            Debug.LogError(
                                $"[FieldGenerator] レイヤー{layer}のノード(Id={f.Id})に NextIds が設定されていません。" +
                                $"レイヤー{layer}とレイヤー{layer + 1}はともにファイルで固定されているため、接続を明示的に記述する必要があります。" );
                            return template;
                        }
                    }
                }
                else
                {
                    // どちらかがFree: システムが自動で接続する(Fixed側の NextIds はここで上書きされる)
                    ConnectLayers( rng, from, to, config.BranchCountRange );
                }
            }

            // レイヤー未指定(Layer < 0)の従来互換ノードも出力には含める(レイヤー接続には参加しない)
            var legacyNodes = nodesById.Values.Where( n =>
                n.Layer < 0 && n.Id != template.StartNodeId && n.Id != template.BossNodeId );

            var allNodes = layerNodes.Values.SelectMany( n => n ).Concat( legacyNodes ).ToArray();

            return new FieldData
            {
                FieldId     = template.FieldId,
                Nodes       = allNodes,
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

        private static FieldNodeData CloneNodeFull( FieldNodeData source )
        {
            return new FieldNodeData
            {
                Id         = source.Id,
                Type       = source.Type,
                Layer      = source.Layer,
                StageIndex = source.StageIndex,
                PosX       = source.PosX,
                PosY       = source.PosY,
                NextIds    = source.NextIds    != null ? ( int[] ) source.NextIds.Clone()    : Array.Empty<int>(),
                PathToNext = source.PathToNext != null ? ( FieldNodePath[] ) source.PathToNext.Clone() : Array.Empty<FieldNodePath>(),
            };
        }
    }
}
