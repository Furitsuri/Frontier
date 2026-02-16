using UnityEngine;

public class ShowIfAttribute : PropertyAttribute
{
    public string conditionName;

    public ShowIfAttribute( string conditionName )
    {
        this.conditionName = conditionName;
    }
}