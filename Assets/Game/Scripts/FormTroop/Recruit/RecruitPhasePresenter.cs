using Frontier.StateMachine;
using Frontier.UI;
using Zenject;

namespace Frontier.FormTroop
{
    /// <summary>
    /// RecruitScene全体を通して使うUI(雇用/解雇選択メニュー・雇用完了確認ダイアログ)への窓口。
    /// 雇用候補一覧の表示自体はTroopEdit形式のグリッド(RecruitRootStateが自前で保持する
    /// TroopEditPresenter/TroopGridController)に移行したため、このクラスはカルーセル関連の
    /// 責務を持たない。
    /// </summary>
    public class RecruitPhasePresenter : PhasePresenterBase, IConfirmPresenter
    {
        private RecruitUISystem _recruitmentUI = null;
        private RecruitTopMenuUI _topMenuUI = null;

        [Inject]
        public RecruitPhasePresenter( IUiSystem uiSystem )
        {
            _uiSystem      = uiSystem;
            _recruitmentUI = _uiSystem.RecruitUi;
            _topMenuUI     = _recruitmentUI.TopMenuUI;
        }

        public void Init()
        {
            _recruitmentUI.gameObject.SetActive( true );
            _recruitmentUI.Init();
        }

        public void Exit()
        {
            _recruitmentUI.Exit();
            _recruitmentUI.gameObject.SetActive( false );
            _topMenuUI.Hide();
        }

        /// <summary>
        /// 雇用/解雇選択メニュー(RecruitTopMenuUI)の表示・非表示を切り替えます
        /// </summary>
        public void SetActiveTopMenu( bool isActive )
        {
            if( isActive ) { _topMenuUI.Show(); }
            else { _topMenuUI.Hide(); }
        }

        /// <summary>
        /// 雇用/解雇選択メニューの選択中インデックスを反映します
        /// </summary>
        public void SetTopMenuSelectedIndex( int index )
        {
            _topMenuUI.SetSelectedIndex( index );
        }

        public void SetActiveConfirmUI( bool isActive )
        {
            _recruitmentUI.ConfirmEmploymentUI.gameObject.SetActive( isActive );
        }

        /// <summary>
        /// 確認ダイアログに表示するメッセージを設定します
        /// </summary>
        /// <param name="message">表示するメッセージ</param>
        public void SetConfirmMessage( string message )
        {
            _recruitmentUI.ConfirmEmploymentUI.SetMessageText( message );
        }

        public void ApplyColor2Options( int selectIndex )
        {
            _recruitmentUI.ConfirmEmploymentUI.ApplyTextColor( selectIndex );
        }
    }
}
