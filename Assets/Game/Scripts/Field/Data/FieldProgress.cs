using System;
using System.Collections.Generic;

namespace Frontier.Field
{
    /// <summary>
    /// フィールド内のプレイ進行状態。セーブデータに含まれる。
    /// </summary>
    [Serializable]
    public class FieldProgress
    {
        public string      FieldId;
        public int         CurrentNodeId;
        public List<int>   ClearedNodeIds = new List<int>();

        /// <summary>
        /// 生成されたノードグラフそのもの(各ノードの位置・種類・繋がり等)。
        /// セーブ/ロードや戦闘・雇用からの帰還時はランダム生成をやり直さず、これをそのまま復元することで
        /// 元のマップ構成を完全に再現する。
        /// </summary>
        public FieldNodeData[] Nodes;
        public int             StartNodeId;
        public int             BossNodeId;

        public bool IsNodeCleared( int nodeId ) => ClearedNodeIds.Contains( nodeId );

        public void MarkCleared( int nodeId )
        {
            if ( !IsNodeCleared( nodeId ) ) ClearedNodeIds.Add( nodeId );
        }
    }
}
