using Zenject;

namespace Frontier.UI
{
    /// <summary>
    /// 画面上部の全幅ヘッダー(GeneralHeaderUI、所持アニマ・部隊人数)の表示を仲介するPresenterです。
    /// 複数の画面(Recruit/Field等)から汎用的に呼ばれるため、各StateやHandlerはIUiSystemに
    /// 直接アクセスせず、必ずこのPresenter経由で表示・更新を行ってください。
    /// 各シーンのDIInstallerでバインドされている前提です(DIInstaller.cs / FieldDiInstaller.cs /
    /// RecruitDiInstaller.cs / TitleDiInstaller.cs 等、IUiSystemが実体のUISystemを指すシーン)。
    /// </summary>
    public class GeneralHeaderPresenter
    {
        [Inject] private IUiSystem _uiSystem = null;

        public void Show() => _uiSystem.GeneralUi.HeaderView.Show();

        public void Hide() => _uiSystem.GeneralUi.HeaderView.Hide();

        public void SetHeaderInfo( int anima, int currentMemberNum, int maxMemberNum )
            => _uiSystem.GeneralUi.HeaderView.SetHeaderInfo( anima, currentMemberNum, maxMemberNum );
    }
}
