using Frontier.Option;
using System.Collections.Generic;
using Zenject;

namespace Frontier.Field
{
    /// <summary>
    /// フィールドメニュー(OPT2)の表示・選択状態を管理するPresenter。
    /// FieldSceneController は本クラスを介してのみ FieldMenuUI を操作する
    /// (BattleRoutinePresenter が BattleUi の各ウィンドウを仲介するのと同様の役割)。
    /// </summary>
    public class FieldMenuPresenter
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;
        [Inject] private OptionHandler _optionHandler        = null;

        private FieldMenuUI _menuUI = null;
        private CommandList _commandList = new CommandList();
        private CommandList.CommandIndexedValue _cmdIdxVal;

        /// <summary>メニューが開いているかどうか。</summary>
        public bool IsOpen { get; private set; }

        /// <summary>
        /// メニューUIを生成します(一度だけ呼び出してください)。
        /// </summary>
        public void Init()
        {
            _menuUI = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<FieldMenuUI>( true, false, nameof( FieldMenuUI ) );
            _menuUI.Setup();
        }

        /// <summary>
        /// メニューを開き、カーソルを先頭項目にリセットします。
        /// </summary>
        public void Show()
        {
            IsOpen = true;

            var indices = new List<int>();
            for ( int i = 0; i < ( int ) FIELD_MENU_OPTION_TAG.NUM; ++i ) { indices.Add( i ); }

            _cmdIdxVal = new CommandList.CommandIndexedValue( 0, 0 );
            _commandList.Init( ref indices, CommandList.CommandDirection.VERTICAL, false, _cmdIdxVal );

            _menuUI.SetSelectedIndex( _cmdIdxVal.index );
            _menuUI.Show();
        }

        /// <summary>
        /// メニューを閉じます。
        /// </summary>
        public void Hide()
        {
            IsOpen = false;
            _menuUI.Hide();
        }

        /// <summary>
        /// IsOpen(メニューを開いた状態)は変更せず、パネルの表示のみ切り替えます。
        /// オプション画面などのサブ画面に一時的に処理を譲る際、復帰後に同じ選択状態のまま
        /// 再表示できるようにするために使用します(完全に閉じたい場合はHide()を使用してください)。
        /// </summary>
        public void SetPanelVisible( bool visible )
        {
            if ( visible )
            {
                _menuUI.SetSelectedIndex( _cmdIdxVal.index );
                _menuUI.Show();
            }
            else
            {
                _menuUI.Hide();
            }
        }

        /// <summary>
        /// カーソルを移動します。移動できた場合はtrueを返します。
        /// </summary>
        public bool MoveSelection( Direction dir )
        {
            if ( !_commandList.OperateListCursor( dir ) ) return false;

            _menuUI.SetSelectedIndex( _cmdIdxVal.index );

            return true;
        }

        /// <summary>
        /// 現在選択中の項目を確定操作します。
        /// </summary>
        /// <returns>この確定操作によって以降FieldMenuHandlerが行うべき処理。</returns>
        public FieldMenuConfirmResult ConfirmSelection()
        {
            var option = ( FIELD_MENU_OPTION_TAG ) _cmdIdxVal.value;
            switch ( option )
            {
                // PlSelectMenuStateのTILE_MENU_OPTION_TAG.OPTION処理を参考に、
                // OptionHandlerへ実行を委譲する(表示・入力操作はOptionHandler/OptionPresenter側が担う)
                case FIELD_MENU_OPTION_TAG.OPTION:
                    _optionHandler.ScheduleRun();
                    return FieldMenuConfirmResult.SuspendForOption;

                case FIELD_MENU_OPTION_TAG.EXIT_GAME:
                    return FieldMenuConfirmResult.RequestExitGameConfirm;

                case FIELD_MENU_OPTION_TAG.SAVE:
                    return FieldMenuConfirmResult.RequestSaveScreen;

                default:
                    // MEMO: 他の項目からの遷移処理は未実装
                    UnityEngine.Debug.Log( $"[FieldMenuPresenter] {option} は未実装です。" );
                    return FieldMenuConfirmResult.None;
            }
        }
    }
}
