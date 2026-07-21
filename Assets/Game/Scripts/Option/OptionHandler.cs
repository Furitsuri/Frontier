using System.Collections.Generic;
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

        // TODO: 実際の音量への反映・設定の永続化は、音響システム側の実装に合わせて別途対応する
        private float _bgmVolume = 100f;
        private float _seVolume = 100f;

        void Start()
        {
            var items = new List<IOptionItem>
            {
                new SliderOptionItem( "BGM", 0f, 100f, _bgmVolume, value => _bgmVolume = value ),
                new SliderOptionItem( "SE",  0f, 100f, _seVolume,  value => _seVolume  = value ),
            };

            LazyInject.GetOrCreate( ref _presenter, () => _hierarchyBld.InstantiateWithDiContainer<OptionPresenter>( false ) );
            _presenter.Init( items );

            _presenter.OnCloseRequested += HandleCloseRequested;

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

            _presenter.Show();

            RegisterActiveInputCode();
        }

        public override void Restart()
        {
            base.Restart();

            _presenter.Show();

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
