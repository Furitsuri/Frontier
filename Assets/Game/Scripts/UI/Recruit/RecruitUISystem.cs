using UnityEngine;

namespace Frontier.UI
{
    public sealed class RecruitUISystem : MonoBehaviour
    {
        [Header( "雇用完了確認UI" )]
        [SerializeField] private ConfirmUI _confirmEmploymentUI;

        [Header( "雇用/解雇選択メニューUI" )]
        [SerializeField] private RecruitTopMenuUI _topMenuUI;

        public ConfirmUI ConfirmEmploymentUI => _confirmEmploymentUI;
        public RecruitTopMenuUI TopMenuUI => _topMenuUI;

        public void Init()
        {
            _confirmEmploymentUI.Init();
            _topMenuUI.Init();

            gameObject.SetActive( true );
            _confirmEmploymentUI.gameObject.SetActive( false );
            // 雇用/解雇選択メニューの表示制御はRecruitTopMenuState側(RecruitPhasePresenter.SetActiveTopMenu)が行う
            _topMenuUI.Hide();
        }

        public void Exit()
        {
            _topMenuUI.Hide();
            gameObject.SetActive( false );
        }
    }
}