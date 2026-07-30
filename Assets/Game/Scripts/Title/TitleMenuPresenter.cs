using Frontier.Option;
using System.Collections.Generic;
using Zenject;

namespace Frontier.Title
{
    /// <summary>
    /// タイトルメニュー(TitleMenuUI)の表示・選択状態を管理するPresenter。
    /// FieldMenuPresenter等と同様、TitleMenuHandler は本クラスを介してのみ TitleMenuUI を操作する。
    /// </summary>
    public class TitleMenuPresenter
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;
        [Inject] private OptionHandler _optionHandler        = null;

        private TitleMenuUI _menuUI = null;
        private CommandList _commandList = new CommandList();
        private CommandList.CommandIndexedValue _cmdIdxVal;

        /// <summary>
        /// メニューUIを生成します(一度だけ呼び出してください)。
        /// </summary>
        public void Init()
        {
            _menuUI = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<TitleMenuUI>( true, false, nameof( TitleMenuUI ) );
            _menuUI.Setup();
        }

        /// <summary>
        /// メニューを表示し、カーソルを先頭項目にリセットします。
        /// isLoadGameVisibleがfalseの場合、LOAD GAME項目は非表示になり、カーソル移動の対象からも除外されます。
        /// </summary>
        public void Show( bool isLoadGameVisible )
        {
            _menuUI.SetLoadGameVisible( isLoadGameVisible );

            var indices = new List<int> { ( int ) TITLE_MENU_OPTION_TAG.NEW_GAME };
            if ( isLoadGameVisible ) { indices.Add( ( int ) TITLE_MENU_OPTION_TAG.LOAD_GAME ); }
            indices.Add( ( int ) TITLE_MENU_OPTION_TAG.OPTION );
            indices.Add( ( int ) TITLE_MENU_OPTION_TAG.EXIT_GAME );

            _cmdIdxVal = new CommandList.CommandIndexedValue( 0, 0 );
            _commandList.Init( ref indices, CommandList.CommandDirection.VERTICAL, false, _cmdIdxVal );

            _menuUI.SetSelectedIndex( _cmdIdxVal.value );
        }

        /// <summary>
        /// カーソル位置は変更せず、パネルの表示のみ切り替えます(オプション画面などのサブ画面から
        /// 復帰する際に、同じ選択状態のまま再表示できるようにするために使用します)。
        /// </summary>
        public void SetPanelVisible( bool visible )
        {
            _menuUI.gameObject.SetActive( visible );

            if ( visible ) { _menuUI.SetSelectedIndex( _cmdIdxVal.value ); }
        }

        /// <summary>
        /// カーソルを移動します。移動できた場合はtrueを返します。
        /// </summary>
        public bool MoveSelection( Direction dir )
        {
            if ( !_commandList.OperateListCursor( dir ) ) return false;

            _menuUI.SetSelectedIndex( _cmdIdxVal.value );

            return true;
        }

        /// <summary>
        /// 現在選択中の項目を返します。
        /// </summary>
        public TITLE_MENU_OPTION_TAG GetSelectedOption() => ( TITLE_MENU_OPTION_TAG ) _cmdIdxVal.value;

        /// <summary>
        /// 現在選択中の項目を確定操作します。
        /// </summary>
        /// <returns>この確定操作によって以降TitleMenuHandlerが行うべき処理。</returns>
        public TitleMenuConfirmResult ConfirmSelection()
        {
            switch ( GetSelectedOption() )
            {
                case TITLE_MENU_OPTION_TAG.NEW_GAME:
                    return TitleMenuConfirmResult.RequestNewGame;

                case TITLE_MENU_OPTION_TAG.LOAD_GAME:
                    return TitleMenuConfirmResult.RequestLoadGame;

                // FieldMenuPresenterと同様、OptionHandlerへ実行を委譲する
                // (表示・入力操作はOptionHandler/OptionPresenter側が担う)
                case TITLE_MENU_OPTION_TAG.OPTION:
                    _optionHandler.ScheduleRun();
                    return TitleMenuConfirmResult.SuspendForOption;

                case TITLE_MENU_OPTION_TAG.EXIT_GAME:
                    return TitleMenuConfirmResult.RequestExitGameConfirm;

                default:
                    return TitleMenuConfirmResult.None;
            }
        }
    }
}
