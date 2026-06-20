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
        /// フィールド生成に使用したシード値。
        /// 戦闘帰還などで再読込する際にこの値で再生成することで、同じマップ構成を再現する。
        /// </summary>
        public int GenerationSeed;

        public bool IsNodeCleared( int nodeId ) => ClearedNodeIds.Contains( nodeId );

        public void MarkCleared( int nodeId )
        {
            if ( !IsNodeCleared( nodeId ) ) ClearedNodeIds.Add( nodeId );
        }
    }
}
