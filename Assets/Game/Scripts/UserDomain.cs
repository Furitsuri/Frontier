using Frontier.Entities;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class UserDomain
{
    [SerializeField] public int Money { get; private set; } = 0;
    [SerializeField] public int StageLevel { get; private set; } = 1;
    [SerializeField] public List<Player> Members { get; private set; } = new List<Player>();

    public void AddMoney( int amount )
    {
        Money += amount;
    }

    public void IncreaseStageLevel()
    {
        StageLevel++;
    }

    public void RecruitMember( Player member )
    {
        Members.Add( member );
    }

    /*
    public IReadOnlyList<Unit> Units => _units;

    private List<Unit> _units;

    public void HireUnit( Unit master )
    {
        if( Money < master.Cost ) return;
        Money -= master.Cost;
        _units.Add( new Unit( master ) );
    }

    public UserData ToSaveData()
    {
        return new PlayerSaveData
        {
            Money = Money,
            OwnedUnits = _units.Select( u => u.ToSaveData() ).ToList()
        };
    }

    public void Load( UserData data )
    {
        Money = data.Money;
        _units = data.OwnedUnits.Select( Unit.FromSaveData ).ToList();
    }
    */
}