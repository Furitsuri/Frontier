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
        public void ConfirmSelection()
        {
            // MEMO: 各項目からの遷移処理は未実装。リストへの項目挿入のみ先行対応する
            var option = ( FIELD_MENU_OPTION_TAG ) _cmdIdxVal.value;
            UnityEngine.Debug.Log( $"[FieldMenuPresenter] {option} は未実装です。" );
        }
    }
}
