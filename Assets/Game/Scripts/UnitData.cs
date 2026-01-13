using System;
using System.Collections.Generic;

[Serializable]
public class UnitData
{
    public int UnitId;
    public int Level;
    public int Exp;
    public List<int> EquippedSkillIds;
}