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

        [Header( "雇用完了確認UI" )]
        [SerializeField] private ConfirmUI _confirmEmploymentUI;

        private TextMeshProUGUI _moneyValueText;

        public EmploymentSelectionUI EmploymentSelectUI => _employmentSelectUI;
        public ConfirmUI ConfirmEmploymentUI => _confirmEmploymentUI;

        public void Init()
        {
            _employmentSelectUI.Init( CharacterSelectionDisplayMode.Camera );
            _confirmEmploymentUI.Init();

            gameObject.SetActive( true );
            _moneyUI.SetActive( true );
            _employmentSelectUI.gameObject.SetActive( true );
            _confirmEmploymentUI.gameObject.SetActive( false );
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