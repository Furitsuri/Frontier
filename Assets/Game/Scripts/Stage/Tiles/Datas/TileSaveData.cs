using Frontier.Stage;
using System;

namespace Frontier.Stage
{
    [Serializable]
    public class TileSaveData
    {
        public bool IsDeployable;
        public float Height;
        public TileType TileType;
    }
}