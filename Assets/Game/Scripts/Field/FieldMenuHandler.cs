using Frontier;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.Field
{
    /// <summary>
    /// フィールドメニュー(OPT2)の入力受付とライフサイクルを担当するハンドラ。
    /// OptionHandlerと同様、メインフロー(FieldSceneController)から分離した専用コンポーネントとして、
    /// 入力コードの登録・解除やFieldMenuPresenterの生成・呼び出しをすべてここに閉じ込める。
    /// </summary>
    public class FieldMenuHandler : MonoBehaviour
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;

        private FieldMenuPresenter        _presenter           = null;
        private FieldExitConfirmPresenter _exitConfirmPresenter = null;
        private FieldPlayerCharacterView  _playerCharacterView = null;
        private int _openHashCode;
        private int _navHashCode;
        private int _exitConfirmHashCode;

        /// <summary>メニューが開いているかどうか。</summary>
        public bool IsOpen => _presenter != null && _presenter.IsOpen;

        /// <summary>
        /// Presenterを生成し、OPT2入力の受付を開始します(一度だけ呼び出してください)。
        /// </summary>
        /// <param name="playerCharacterView">移動中かどうかの判定に用いる、自身を表す3Dモデルのビュー</param>
        public void Setup( FieldPlayerCharacterView playerCharacterView )
        {
            _playerCharacterView = playerCharacterView;

            _presenter = _hierarchyBld.InstantiateWithDiContainer<FieldMenuPresenter>( false );
            _presenter.Init();

            _openHashCode = Hash.GetStableHash( nameof( FieldMenuHandler ) + "_Open" );
            InputFacade.Instance.RegisterInputCodes(
                ( GuideIcon.OPT2, "MENU", CanOpen, new AcceptContextInput( AcceptOpen ), 0.0f, _openHashCode )
            );
        }

        /// <summary>
        /// オプション画面等のサブ画面から復帰した際(FieldSceneController.Restart())に呼び出してください。
        /// メニューが開いたままであれば、パネルと入力コードを同じ選択状態で復帰させます。
        /// </summary>
        public void NotifySceneResumed()
        {
            if ( _presenter == null || !_presenter.IsOpen ) return;

            _presenter.SetPanelVisible( true );
            RegisterNavInputCodes();
        }

        /// <summary>
        /// メニューを開けるかどうかを判定します。移動中、既にメニューが開いている場合、
        /// 及びオプション画面等の優先度の高いルーチンが実行中で自身が中断されている場合は開けません。
        /// </summary>
        private bool CanOpen()
        {
            return gameObject.activeInHierarchy &&
                   _presenter != null && !_presenter.IsOpen &&
                   _playerCharacterView != null && !_playerCharacterView.IsMoving;
        }

        private bool AcceptOpen( InputContext context )
        {
            if ( !context.GetButton( GameButton.Opt2 ) ) return false;

            _presenter.Show();
            RegisterNavInputCodes();

            return true;
        }

        /// <summary>
        /// メニュー内でのカーソル移動・決定・キャンセルに対応する入力コードを登録します。
        /// 初回オープン時に加え、サブ画面から復帰した際の再登録にも使用します。
        /// </summary>
        private void RegisterNavInputCodes()
        {
            _navHashCode = Hash.GetStableHash( nameof( FieldMenuHandler ) + "_Nav" );
            InputFacade.Instance.RegisterInputCodes(
                ( GuideIcon.VERTICAL_CURSOR, "SELECT",  InputFacade.CanBeAcceptAlways, new AcceptContextInput( AcceptDirection ), MENU_DIRECTION_INPUT_INTERVAL, _navHashCode ),
                ( GuideIcon.CONFIRM,         "CONFIRM", InputFacade.CanBeAcceptAlways, new AcceptContextInput( AcceptConfirm ),   0.0f, _navHashCode ),
                ( GuideIcon.CANCEL,          "BACK",    InputFacade.CanBeAcceptAlways, new AcceptContextInput( AcceptCancel ),    0.0f, _navHashCode )
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
                // OPTION選択時、オプション画面へ処理を譲る。IsOpenは維持したままパネルと入力コードのみ
                // 一時的に解除する(NotifySceneResumed()により復帰時に自動的に再表示される)
                case FieldMenuConfirmResult.SuspendForOption:
                    SuspendMenuForSubScreen();
                    break;

                // EXIT_GAME選択時、終了確認ダイアログを表示する
                case FieldMenuConfirmResult.RequestExitGameConfirm:
                    SuspendMenuForSubScreen();
                    OpenExitConfirm();
                    break;
            }

            return true;
        }

        private bool AcceptCancel( InputContext context )
        {
            if ( !context.GetButton( GameButton.Cancel ) ) return false;

            Close();

            return true;
        }

        private void Close()
        {
            _presenter.Hide();

            InputFacade.Instance.UnregisterInputCodes( _navHashCode );

            // UnregisterInputCodesだけでは入力ガイド表示が更新されないため、空登録でガイドの再描画を促す
            InputFacade.Instance.RegisterInputCodes();
        }

        /// <summary>
        /// メニュー自体は開いたまま(IsOpen維持)、パネルとナビゲーション入力コードのみ一時的に解除します。
        /// オプション画面・終了確認ダイアログ等のサブ画面へ処理を譲る際に使用します。
        /// </summary>
        private void SuspendMenuForSubScreen()
        {
            _presenter.SetPanelVisible( false );

            InputFacade.Instance.UnregisterInputCodes( _navHashCode );

            // UnregisterInputCodesだけでは入力ガイド表示が更新されないため、空登録でガイドの再描画を促す
            InputFacade.Instance.RegisterInputCodes();
        }

        // ── ゲーム終了確認ダイアログ ─────────────────────────────────────────

        private void EnsureExitConfirmPresenter()
        {
            if ( _exitConfirmPresenter != null ) return;

            _exitConfirmPresenter = _hierarchyBld.InstantiateWithDiContainer<FieldExitConfirmPresenter>( false );
            _exitConfirmPresenter.Init();
        }

        private void OpenExitConfirm()
        {
            EnsureExitConfirmPresenter();

            _exitConfirmPresenter.Show();

            _exitConfirmHashCode = Hash.GetStableHash( nameof( FieldMenuHandler ) + "_ExitConfirm" );
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
        /// 終了確認ダイアログを閉じ、フィールドメニューへ戻ります(「いいえ」及びキャンセル共通の処理)。
        /// </summary>
        private void CloseExitConfirmAndReturnToMenu()
        {
            _exitConfirmPresenter.Hide();
            InputFacade.Instance.UnregisterInputCodes( _exitConfirmHashCode );

            _presenter.SetPanelVisible( true );
            RegisterNavInputCodes();
        }
    }
}
