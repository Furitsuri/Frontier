public struct WaypointInformation
{
    public WaypointInformation( int index, int cost )
    {
        TileIndex   = index;
        MoveCost    = cost;
    }

    public int TileIndex;
    public int MoveCost;
}