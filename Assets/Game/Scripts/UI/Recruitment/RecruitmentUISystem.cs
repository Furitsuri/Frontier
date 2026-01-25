using TMPro;
using UnityEngine;

public sealed class RecruitmentUISystem : MonoBehaviour
{
    [Header( "所持金UI" )]
    [SerializeField] private GameObject _moneyUI;

    [Header( "雇用ユニット選択UI" )]
    [SerializeField] private GameObject _unitSelectUI;

    private TextMeshProUGUI _moneyValueText;

    public void Init()
    {
        gameObject.SetActive( true );
    }

    public void Exit()
    {
        gameObject.SetActive( false );
    }

    public void Setup()
    {
        _moneyValueText = _moneyUI.GetComponentInChildren<TextMeshProUGUI>();
    }

    public void SetMoneyValue( float value )
    {
        _moneyValueText.text = value.ToString();
    }
}
