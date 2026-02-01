using TMPro;
using UnityEngine;

namespace Frontier.UI
{
    public sealed class RecruitUISystem : MonoBehaviour
    {
        [Header( "所持金UI" )]
        [SerializeField] private GameObject _moneyUI;

        [Header( "雇用ユニット選択UI" )]
        [SerializeField] private EmploymentSelectionUI _employmentSelectUI;

        private TextMeshProUGUI _moneyValueText;

        public EmploymentSelectionUI EmploymentSelectUI => _employmentSelectUI;

        public void Init()
        {
            _employmentSelectUI.Init( CharacterSelectionDisplayMode.Camera );

            gameObject.SetActive( true );
            _moneyUI.SetActive( true );
            _employmentSelectUI.gameObject.SetActive( true );
        }

        public void Exit()
        {
            _employmentSelectUI.gameObject.SetActive( false );
            _moneyUI.SetActive( false );
            gameObject.SetActive( false );
        }

        public void Setup()
        {
            _employmentSelectUI.Setup();
            _moneyValueText = _moneyUI.GetComponentInChildren<TextMeshProUGUI>();
        }

        public void SetMoneyValue( float value )
        {
            _moneyValueText.text = value.ToString();
        }
    }
}