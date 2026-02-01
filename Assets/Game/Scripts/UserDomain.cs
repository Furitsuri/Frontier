using System.Collections.Generic;

public class UserDomain
{
    public int Money { get; private set; }
    public int StageLevel { get; private set; }
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