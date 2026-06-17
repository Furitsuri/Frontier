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

        public bool IsNodeCleared( int nodeId ) => ClearedNodeIds.Contains( nodeId );

        public void MarkCleared( int nodeId )
        {
            if ( !IsNodeCleared( nodeId ) ) ClearedNodeIds.Add( nodeId );
        }
    }
}
