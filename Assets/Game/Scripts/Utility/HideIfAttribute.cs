using UnityEngine;

public class HideIfAttribute : PropertyAttribute
{
    public string conditionName;

    public HideIfAttribute( string conditionName )
    {
        this.conditionName = conditionName;
    }
}