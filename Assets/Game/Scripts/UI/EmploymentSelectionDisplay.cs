using Frontier.Entities;
using Frontier.UI;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class EmploymentSelectionDisplay : CharacterSelectionDisplay
{
    [Header( "雇用チェックマークオブジェクト" )]
    [SerializeField] private GameObject _employedMarkObject;

    [Header( "雇用コストオブジェクト" )]
    [SerializeField] private GameObject _costObject;

    [Header( "雇用コストテキスト" )]
    [SerializeField] private TextMeshProUGUI _tmpEmploymentCostValue;

    private Color _originalCostSpriteColor;
    private Color _originalCostTextColor;

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

        _originalCostSpriteColor    = _costObject.GetComponentInChildren<Image>().color;
        _originalCostTextColor      = _tmpEmploymentCostValue.color;
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

    public override void SetFocusedColor( bool isFocused )
    {
        base.SetFocusedColor( isFocused );

        if( !isFocused )
        {
            _costObject.GetComponentInChildren<Image>().color = ToGray( _originalCostSpriteColor );
            _tmpEmploymentCostValue.color = ToGray( _originalCostTextColor );
        }
        else
        {
            _costObject.GetComponentInChildren<Image>().color = _originalCostSpriteColor;
            _tmpEmploymentCostValue.color = _originalCostTextColor;
        }
    }

    private Color ToGray( Color srcColor )
    {
        float gray = srcColor.grayscale;
        Color grayColor = new Color( gray, gray, gray, srcColor.a );

        return grayColor * 0.6f;
    }
}