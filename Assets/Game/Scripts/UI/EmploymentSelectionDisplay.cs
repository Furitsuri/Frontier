using Frontier.Entities;
using Frontier.UI;
using System.Collections;
using TMPro;
using UnityEngine;

public sealed class EmploymentSelectionDisplay : CharacterSelectionDisplay
{
    [Header( "雇用コストオブジェクト" )]
    [SerializeField] private GameObject _costObject;

    [Header( "雇用コストテキスト" )]
    [SerializeField] private TextMeshProUGUI _tmpEmploymentCostValue;

    public TextMeshProUGUI EmploymentCostValue => _tmpEmploymentCostValue;

    public void SetActiveCostObject( bool isActive )
    {         
        _costObject.SetActive( isActive );
    }

    public void SetCostValueText( int value )
    {
        _tmpEmploymentCostValue.text = value.ToString();
    }

    public override void AssignSelectCandidate( ref CharacterCandidate candidate )
    {
        base.AssignSelectCandidate( ref candidate );

        Player player = candidate.Character as Player;
        NullCheck.AssertNotNull( player, nameof( player ) );
        SetCostValueText( player.RecruitLogic.Cost );
    }
}