using System.Collections.Generic;
using Zenject;
using static Constants;

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
        [Inject] private ISaveHandler<OptionSaveData> _saveHdlr = null;

        private OptionPresenter _presenter = null;
        private OptionSaveData _saveData = null;

        void Start()
        {
            _saveData = _saveHdlr.Load();

            // 読み込んだ設定をすぐさま反映する(BGM/SEは音響システム側の実装に合わせて別途対応する)
            _inputFcd.SetGuideVisible( _saveData.IsInputGuideVisible );

            var items = new List<IOptionItem>
            {
                new SliderOptionItem( "BGM", 0f, 100f, _saveData.BgmVolume, value => _saveData.BgmVolume = value ),
                new SliderOptionItem( "SE",  0f, 100f, _saveData.SeVolume,  value => _saveData.SeVolume  = value ),
                new ToggleOptionItem( "INPUT GUIDE", _saveData.IsInputGuideVisible, value =>
                {
                    _saveData.IsInputGuideVisible = value;
                    _inputFcd.SetGuideVisible( value );
                } ),
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
                (GuideIcon.VERTICAL_CURSOR,   "SELECT",  CanAcceptVertical,   new AcceptContextInput( AcceptVertical ),   MENU_DIRECTION_INPUT_INTERVAL, hashCode),
                (GuideIcon.HORIZONTAL_CURSOR, "ADJUST",  CanAcceptHorizontal, new AcceptContextInput( AcceptHorizontal ), 0.06f, hashCode),
                (GuideIcon.CONFIRM,           "CONFIRM", CanAcceptConfirm,    new AcceptContextInput( AcceptConfirm ),    0.0f, hashCode),
                (GuideIcon.CANCEL,            "CLOSE",   CanAcceptCancel,     new AcceptContextInput( AcceptCancel ),     0.0f, hashCode)
             );
        }

        private bool CanAcceptVertical()
        {
            return IsMatchFocusState( FocusState.RUN );
        }

        /// <summary>
        /// 現在選択中の項目がスライダーの場合のみ、左右入力を受け付けます
        /// </summary>
        private bool CanAcceptHorizontal()
        {
            return IsMatchFocusState( FocusState.RUN ) && _presenter.IsSliderSelected();
        }

        private bool CanAcceptConfirm()
        {
            return IsMatchFocusState( FocusState.RUN );
        }

        private bool CanAcceptCancel()
        {
            return IsMatchFocusState( FocusState.RUN );
        }

        /// <summary>
        /// 上下入力を受け、選択中の項目を移動します(末尾・先頭で循環)
        /// </summary>
        private bool AcceptVertical( InputContext context )
        {
            switch( context.Cursor )
            {
                case Direction.FORWARD:
                    _presenter.MoveSelection( -1 );
                    return true;

                case Direction.BACK:
                    _presenter.MoveSelection( 1 );
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 左右入力を受け、選択中のスライダーの値を調整します
        /// </summary>
        private bool AcceptHorizontal( InputContext context )
        {
            switch( context.Cursor )
            {
                case Direction.LEFT:
                    _presenter.AdjustSelectedSlider( -1f );
                    return true;

                case Direction.RIGHT:
                    _presenter.AdjustSelectedSlider( 1f );
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 決定入力を受け、選択中の項目を確定操作します(トグルの切替・閉じるボタンの実行)
        /// </summary>
        private bool AcceptConfirm( InputContext context )
        {
            if( !context.GetButton( GameButton.Confirm ) ) { return false; }

            _presenter.ConfirmSelection();

            return true;
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
        /// 処理を停止します。この時点の設定内容を保存します
        /// </summary>
        public override void Exit()
        {
            base.Exit();

            _presenter.Hide();
            _saveHdlr.Save( _saveData );

            _inputFcd.UnregisterInputCodes( Hash.GetStableHash( GetType().Name ) );
        }

        public override int GetPriority() { return ( int ) FocusRoutinePriority.OPTIONS; }

        #endregion
    }
}
