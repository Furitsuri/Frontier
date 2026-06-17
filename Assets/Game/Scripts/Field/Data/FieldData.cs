using System;

namespace Frontier.Field
{
    [Serializable]
    public class FieldData
    {
        public string        FieldId;
        public FieldNodeData[] Nodes;
        public int           StartNodeId;
        public int           BossNodeId;
    }
}
