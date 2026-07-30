using Frontier.SaveLoad;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;
using static Constants;

namespace Frontier.Title
{
    /// <summary>
    /// タイトルメニューの入力受付とライフサイクルを担当するハンドラ。
    /// TitleMainのFocusRoutineController配下でMAIN_FLOWとして登録されるFocusRoutineBase。
    /// InputFacadeのセットアップはTitleMain(FocusRoutineController.Awake())が行うため、
    /// このクラスではRegisterInputCodes()のみを行う。
    /// 起動時にセーブデータの有無を確認し、いずれかのスロットにデータが存在する場合のみ
    /// LOAD GAME項目を表示します。
    /// MEMO: LOAD GAME選択後の実際のロード処理(DTOの内容を反映してのシーン遷移)は未実装。
    /// 画面表示・スロット選択までを今回のスコープとする。
    /// </summary>
    public class TitleMenuHandler : FocusRoutineBase
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;
        [Inject] private ISlotSaveHandler<UserSaveData> _saveHdlr = null;

        private TitleMenuPresenter _presenter = null;
        private YesNoConfirmPresenter _exitConfirmPresenter = null;
        private SaveLoadHandler _saveLoadHandler = null;
        private int _navHashCode;
        private int _exitConfirmHashCode;

        /// <summary>
        /// FocusRoutine優先度制御用。MAIN_FLOWとして登録する。
        /// </summary>
        public override int GetPriority() => ( int ) FocusRoutinePriority.MAIN_FLOW;

        private void Start()
        {
            _presenter = _hierarchyBld.InstantiateWithDiContainer<TitleMenuPresenter>( false );
            _presenter.Init();

            _presenter.Show( CheckHasAnySaveData() );

            RegisterNavInputCodes();
        }

        /// <summary>
        /// オプション画面等、優先度の高いルーチンに処理が移った際にFocusRoutineControllerから
        /// 自動的に呼ばれます。パネルと入力コードを一時的に隠します。
        /// </summary>
        public override void Pause()
        {
            base.Pause();

            _presenter.SetPanelVisible( false );

            InputFacade.Instance.UnregisterInputCodes( _navHashCode );
            InputFacade.Instance.RegisterInputCodes();
        }

        /// <summary>
        /// オプション画面等から復帰した際にFocusRoutineControllerから自動的に呼ばれます。
        /// 同じ選択状態のままメニューを再表示します。
        /// </summary>
        public override void Restart()
        {
            base.Restart();

            _presenter.SetPanelVisible( true );
            RegisterNavInputCodes();
        }

        /// <summary>
        /// いずれかのセーブスロット(オートセーブを含む)にデータが存在するかを判定します。
        /// </summary>
        private bool CheckHasAnySaveData()
        {
            for ( int slot = 0; slot < USER_SAVE_SLOT_COUNT; ++slot )
            {
                if ( _saveHdlr.Exists( slot ) ) return true;
            }

            return false;
        }

        /// <summary>
        /// メニュー内でのカーソル移動・決定に対応する入力コードを登録します。
        /// 初回起動時に加え、サブ画面から復帰した際の再登録にも使用します。
        /// </summary>
        private void RegisterNavInputCodes()
        {
            _navHashCode = Hash.GetStableHash( nameof( TitleMenuHandler ) + "_Nav" );
            InputFacade.Instance.RegisterInputCodes(
                ( GuideIcon.VERTICAL_CURSOR, "SELECT",  InputFacade.CanBeAcceptAlways, new AcceptContextInput( AcceptDirection ), MENU_DIRECTION_INPUT_INTERVAL, _navHashCode ),
                ( GuideIcon.CONFIRM,         "CONFIRM", InputFacade.CanBeAcceptAlways, new AcceptContextInput( AcceptConfirm ),   0.0f, _navHashCode )
            );
        }

        private bool AcceptDirection( InputContext context )
        {
            return _presenter.MoveSelection( context.Cursor );
        }

        private bool AcceptConfirm( InputContext context )
        {
            if ( !context.GetButton( GameButton.Confirm ) ) return false;

            switch ( _presenter.ConfirmSelection() )
            {
                case TitleMenuConfirmResult.RequestNewGame:
                    StartNewGame();
                    break;

                case TitleMenuConfirmResult.RequestLoadGame:
                    SuspendMenuForSubScreen();
                    OpenLoadScreen();
                    break;

                // OPTION選択時のパネル・入力コードの中断/復帰は、Pause()/Restart()のオーバーライドが
                // FocusRoutineControllerの優先度制御から自動的に呼ばれることで行われるため、ここでは何もしない
                case TitleMenuConfirmResult.SuspendForOption:
                    break;

                case TitleMenuConfirmResult.RequestExitGameConfirm:
                    SuspendMenuForSubScreen();
                    OpenExitConfirm();
                    break;
            }

            return true;
        }

        private void StartNewGame()
        {
            SceneManager.LoadScene( "BattleScene" );
        }

        /// <summary>
        /// パネルと入力コードのみ一時的に解除します(終了確認ダイアログ等のサブ画面へ処理を譲る際に使用)。
        /// </summary>
        private void SuspendMenuForSubScreen()
        {
            _presenter.SetPanelVisible( false );

            InputFacade.Instance.UnregisterInputCodes( _navHashCode );
            InputFacade.Instance.RegisterInputCodes();
        }

        /// <summary>
        /// 終了確認ダイアログを閉じてタイトルメニューへ戻ります。パネルと入力コードを同じ選択状態のまま再表示します。
        /// </summary>
        private void ReturnToTitleMenu()
        {
            _presenter.SetPanelVisible( true );
            RegisterNavInputCodes();
        }

        // ── ロード画面 ───────────────────────────────────────────────────────

        private void EnsureSaveLoadHandler()
        {
            if ( _saveLoadHandler != null ) return;

            _saveLoadHandler = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<SaveLoadHandler>( true, false, nameof( SaveLoadHandler ) );
            _saveLoadHandler.Setup();
        }

        private void OpenLoadScreen()
        {
            EnsureSaveLoadHandler();

            _saveLoadHandler.Show( SaveLoadMode.Load, ReturnToTitleMenu );
        }

        // ── ゲーム終了確認ダイアログ ─────────────────────────────────────────

        private void EnsureExitConfirmPresenter()
        {
            if ( _exitConfirmPresenter != null ) return;

            _exitConfirmPresenter = _hierarchyBld.InstantiateWithDiContainer<YesNoConfirmPresenter>( false );
            _exitConfirmPresenter.Init();
        }

        private void OpenExitConfirm()
        {
            EnsureExitConfirmPresenter();

            _exitConfirmPresenter.Show( "UI_CONFIRM_EXIT_GAME_MESSAGE" );

            _exitConfirmHashCode = Hash.GetStableHash( nameof( TitleMenuHandler ) + "_ExitConfirm" );
            InputFacade.Instance.RegisterInputCodes(
                ( GuideIcon.HORIZONTAL_CURSOR, "SELECT",  InputFacade.CanBeAcceptAlways, new AcceptContextInput( AcceptExitConfirmDirection ), MENU_DIRECTION_INPUT_INTERVAL, _exitConfirmHashCode ),
                ( GuideIcon.CONFIRM,           "CONFIRM", InputFacade.CanBeAcceptAlways, new AcceptContextInput( AcceptExitConfirmConfirm ),   0.0f, _exitConfirmHashCode ),
                ( GuideIcon.CANCEL,            "BACK",    InputFacade.CanBeAcceptAlways, new AcceptContextInput( AcceptExitConfirmCancel ),    0.0f, _exitConfirmHashCode )
            );
        }

        private bool AcceptExitConfirmDirection( InputContext context )
        {
            return _exitConfirmPresenter.MoveSelection( context.Cursor );
        }

        private bool AcceptExitConfirmConfirm( InputContext context )
        {
            if ( !context.GetButton( GameButton.Confirm ) ) return false;

            if ( _exitConfirmPresenter.IsYesSelected() )
            {
                GameQuitService.Quit();
            }
            else
            {
                CloseExitConfirmAndReturnToMenu();
            }

            return true;
        }

        private bool AcceptExitConfirmCancel( InputContext context )
        {
            if ( !context.GetButton( GameButton.Cancel ) ) return false;

            CloseExitConfirmAndReturnToMenu();

            return true;
        }

        /// <summary>
        /// 終了確認ダイアログを閉じ、タイトルメニューへ戻ります(「いいえ」及びキャンセル共通の処理)。
        /// </summary>
        private void CloseExitConfirmAndReturnToMenu()
        {
            _exitConfirmPresenter.Hide();
            InputFacade.Instance.UnregisterInputCodes( _exitConfirmHashCode );

            ReturnToTitleMenu();
        }
    }
}
