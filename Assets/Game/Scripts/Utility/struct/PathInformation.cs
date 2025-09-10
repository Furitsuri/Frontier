public struct PathInformation
{
    public PathInformation( int index, int cost )
    {
        TileIndex   = index;
        MoveCost    = cost;
    }

    public int TileIndex;
    public int MoveCost;
}