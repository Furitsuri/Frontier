using Frontier.Entities;
using Frontier.UI;
using System.Collections;
using TMPro;
using UnityEngine;

public sealed class EmploymentSelectionDisplay : CharacterSelectionDisplay
{
    [Header( "雇用チェックマークオブジェクト" )]
    [SerializeField] private GameObject _employedMarkObject;

    [Header( "雇用コストオブジェクト" )]
    [SerializeField] private GameObject _costObject;

    [Header( "雇用コストテキスト" )]
    [SerializeField] private TextMeshProUGUI _tmpEmploymentCostValue;

    public void SetActiveEmployedMarkObject( bool isActive )
    {
        _employedMarkObject.SetActive( isActive );
    }

    public void SetActiveCostObject( bool isActive )
    {         
        _costObject.SetActive( isActive );
    }

    public void SetCostValueText( int value )
    {
        _tmpEmploymentCostValue.text = value.ToString();
    }

    public override void Setup()
    {
        base.Setup();

        SetActiveEmployedMarkObject( false );
        SetActiveCostObject( true );
    }

    /// <summary>
    /// キャラクター候補の情報を表示に反映します
    /// </summary>
    /// <param name="candidate"></param>
    public override void AssignSelectCandidate( ref CharacterCandidate candidate )
    {
        base.AssignSelectCandidate( ref candidate );

        Player player = candidate.Character as Player;
        NullCheck.AssertNotNull( player, nameof( player ) );
        SetActiveEmployedMarkObject( player.RecruitLogic.IsEmployed );
        SetCostValueText( player.RecruitLogic.Cost );
    }
}