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

            _cmdIdxVal = new CommandList.CommandIndexedValue( 0, 0 );
            _commandList.Init( ref indices, CommandList.CommandDirection.VERTICAL, false, _cmdIdxVal );

            _menuUI.SetSelectedIndex( _cmdIdxVal.value );
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
    }
}
