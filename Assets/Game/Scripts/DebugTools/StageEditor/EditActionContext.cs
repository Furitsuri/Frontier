using System.Collections.Generic;

public struct EditActionContext
{
    public int X;
    public int Y;
    public List<int> ExtraIntValues;

    public void Setup()
    {
        ExtraIntValues = new List<int>();
    }
}