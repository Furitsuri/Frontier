using System;
using System.Collections.Generic;

[Serializable]
public class UserData
{
    public int Money;
    public List<UnitData> OwnedUnits;
    public int CurrentStageId;
    public Dictionary<string, bool> Flags; // 解放状態など
}