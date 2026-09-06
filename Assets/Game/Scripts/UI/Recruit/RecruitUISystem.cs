using TMPro;
using UnityEngine;

namespace Frontier.UI
{
    public sealed class RecruitUISystem : MonoBehaviour
    {
        [Header( "所持アニマUI" )]
        [SerializeField] private GameObject _moneyUI;

        [Header( "雇用ユニット選択UI" )]
        [SerializeField] private RecruitSelectionUI _employmentSelectUI;

        [Header( "雇用完了確認UI" )]
        [SerializeField] private ConfirmUI _confirmEmploymentUI;

        [Header( "雇用/解雇選択メニューUI" )]
        [SerializeField] private RecruitTopMenuUI _topMenuUI;

        private TextMeshProUGUI _moneyValueText;

        public RecruitSelectionUI EmploymentSelectUI => _employmentSelectUI;
        public ConfirmUI ConfirmEmploymentUI => _confirmEmploymentUI;
        public RecruitTopMenuUI TopMenuUI => _topMenuUI;

        public void Init()
        {
            _employmentSelectUI.Init();
            _confirmEmploymentUI.Init();
            _topMenuUI.Init();

            gameObject.SetActive( true );
            _moneyUI.SetActive( true );
            // 中央のキャラクター選択ウィンドウは「雇用」選択後(RecruitRootState.Init())に表示されるため、
            // ここでは有効化しない(_employmentSelectUI.Init()が末尾でSetActive(false)している)
            _confirmEmploymentUI.gameObject.SetActive( false );
            // 雇用/解雇選択メニューの表示制御はRecruitTopMenuState側(RecruitPhasePresenter.SetActiveTopMenu)が行う
            _topMenuUI.Hide();
        }

        public void Exit()
        {
            _employmentSelectUI.gameObject.SetActive( false );
            _moneyUI.SetActive( false );
            _topMenuUI.Hide();
            gameObject.SetActive( false );
        }

        public void Setup()
        {
            _employmentSelectUI.Setup( CharacterSelectionDisplayMode.Camera );
            _moneyValueText = _moneyUI.GetComponentInChildren<TextMeshProUGUI>();
        }

        public void SetAnimaValue( float value )
        {
            _moneyValueText.text = value.ToString();
        }
    }
}