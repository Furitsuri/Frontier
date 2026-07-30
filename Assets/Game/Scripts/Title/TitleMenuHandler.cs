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
    /// MEMO: LOAD GAME選択後の実際のロード処理・シーン遷移は今回のスコープ外(表示/非表示の判定のみ対応)。
    /// </summary>
    public class TitleMenuHandler : FocusRoutineBase
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;
        [Inject] private ISlotSaveHandler<UserSaveData> _saveHdlr = null;

        private TitleMenuPresenter _presenter = null;
        private int _navHashCode;

        /// <summary>
        /// FocusRoutine優先度制御用。MAIN_FLOWとして登録する。
        /// </summary>
        public override int GetPriority() => ( int ) FocusRoutinePriority.MAIN_FLOW;

        private void Start()
        {
            _presenter = _hierarchyBld.InstantiateWithDiContainer<TitleMenuPresenter>( false );
            _presenter.Init();

            _presenter.Show( CheckHasAnySaveData() );

            _navHashCode = Hash.GetStableHash( nameof( TitleMenuHandler ) + "_Nav" );
            InputFacade.Instance.RegisterInputCodes(
                ( GuideIcon.VERTICAL_CURSOR, "SELECT",  InputFacade.CanBeAcceptAlways, new AcceptContextInput( AcceptDirection ), MENU_DIRECTION_INPUT_INTERVAL, _navHashCode ),
                ( GuideIcon.CONFIRM,         "CONFIRM", InputFacade.CanBeAcceptAlways, new AcceptContextInput( AcceptConfirm ),   0.0f, _navHashCode )
            );
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

        private bool AcceptDirection( InputContext context )
        {
            return _presenter.MoveSelection( context.Cursor );
        }

        private bool AcceptConfirm( InputContext context )
        {
            if ( !context.GetButton( GameButton.Confirm ) ) return false;

            switch ( _presenter.GetSelectedOption() )
            {
                case TITLE_MENU_OPTION_TAG.NEW_GAME:
                    StartNewGame();
                    break;

                case TITLE_MENU_OPTION_TAG.LOAD_GAME:
                    // MEMO: セーブデータの反映・シーン遷移は未実装(今回は表示/非表示判定のみが対応範囲のため)
                    Debug.Log( "[TitleMenuHandler] LOAD GAMEの実処理は未実装です。" );
                    break;
            }

            return true;
        }

        private void StartNewGame()
        {
            SceneManager.LoadScene( "BattleScene" );
        }
    }
}
