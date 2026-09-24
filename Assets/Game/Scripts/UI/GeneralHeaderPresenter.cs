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

        /// <summary>
        /// 所持アニマの増減差分を、アニマ数値のすぐ下に表示します(雇用/解雇画面での予備登録、ショップの購入予定額)。
        /// 0を渡すと非表示にします。emphasizedをtrueにすると文字を大きくして目立たせます(購入確認画面等)。
        /// </summary>
        public void SetAnimaDiff( int diff, bool emphasized = false ) => _uiSystem.GeneralUi.HeaderView.SetAnimaDiff( diff, emphasized );

        /// <summary>
        /// 左端に現在の画面(State)タイトルを表示します(雇用/解雇/部隊編集/ステージ番号表示等)。
        /// </summary>
        public void SetStateTitle( LocKey key, params object[] args ) => _uiSystem.GeneralUi.HeaderView.SetStateTitle( key, args );

        /// <summary>
        /// 左端の画面タイトル表示を消去します(該当する画面を離れた際に呼び出してください)。
        /// </summary>
        public void ClearStateTitle() => _uiSystem.GeneralUi.HeaderView.ClearStateTitle();
    }
}
