using Zenject;

namespace Frontier.Option
{
    /// <summary>
    /// オプション画面の開閉と入力フォーカスの取得/解放を担うFocusRoutine。
    /// 呼び出しは常時有効なグローバルホットキーにはせず、各画面(タイトルのメニュー選択、
    /// PlSelectTileStateのOPT1入力など)が「安全なタイミングである」と判断した上で
    /// 明示的に ScheduleRun() を呼び出す形を想定しています。
    /// 実際のUI操作はOptionPresenterに委譲します。
    /// </summary>
    public class OptionHandler : BaseHandlerExtendedFocusRoutine
    {
        [Inject] private InputFacade _inputFcd = null;
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;

        private OptionPresenter _presenter = null;

        // TODO: BGM/SE音量の永続化・実際の音量への反映は、音響システム側の実装に合わせて別途対応する
        private float _bgmVolume = 1.0f;
        private float _seVolume = 1.0f;

        void Start()
        {
            LazyInject.GetOrCreate( ref _presenter, () => _hierarchyBld.InstantiateWithDiContainer<OptionPresenter>( false ) );
            _presenter.Init();

            _presenter.OnCloseRequested += HandleCloseRequested;
            _presenter.OnBgmVolumeChanged += value => _bgmVolume = value;
            _presenter.OnSeVolumeChanged += value => _seVolume = value;

            base.Init();
        }

        /// <summary>
        /// オプション画面操作中の入力コードを登録します
        /// </summary>
        private void RegisterActiveInputCode()
        {
            int hashCode = Hash.GetStableHash( GetType().Name );

            _inputFcd.RegisterInputCodes(
                (GuideIcon.CANCEL, "CLOSE", CanAcceptCancel, new AcceptContextInput( AcceptCancel ), 0.0f, hashCode)
             );
        }

        private bool CanAcceptCancel()
        {
            return IsMatchFocusState( FocusState.RUN );
        }

        private bool AcceptCancel( InputContext context )
        {
            if( !context.GetButton( GameButton.Cancel ) ) { return false; }

            ScheduleExit();

            return true;
        }

        private void HandleCloseRequested()
        {
            ScheduleExit();
        }

        // =========================================================
        // IFocusRoutine 実装
        // =========================================================
        #region IFocusRoutine Implementation

        public override void Run()
        {
            base.Run();

            _presenter.Show( _bgmVolume, _seVolume );

            RegisterActiveInputCode();
        }

        public override void Restart()
        {
            base.Restart();

            _presenter.Show( _bgmVolume, _seVolume );

            RegisterActiveInputCode();
        }

        /// <summary>
        /// IFocusRoutineの実装です
        /// 処理を中断します
        /// </summary>
        public override void Pause()
        {
            base.Pause();

            _inputFcd.UnregisterInputCodes( Hash.GetStableHash( GetType().Name ) );
        }

        /// <summary>
        /// IFocusRoutineの実装です
        /// 処理を停止します
        /// </summary>
        public override void Exit()
        {
            base.Exit();

            _presenter.Hide();

            _inputFcd.UnregisterInputCodes( Hash.GetStableHash( GetType().Name ) );
        }

        public override int GetPriority() { return ( int ) FocusRoutinePriority.OPTIONS; }

        #endregion
    }
}
