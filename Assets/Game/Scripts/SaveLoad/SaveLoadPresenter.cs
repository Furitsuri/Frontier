using Frontier.UI;
using System.Collections.Generic;
using Zenject;

namespace Frontier.SaveLoad
{
    /// <summary>
    /// セーブ/ロード画面(SaveLoadUI)の表示・選択状態を管理するPresenter。
    /// SaveLoadHandler は本クラスを介してのみ SaveLoadUI を操作する。
    /// 実際のセーブ・ロード処理(ファイルI/O)はまだ実装しない(セーブスロットの概念が未確定のため)。
    /// </summary>
    public class SaveLoadPresenter
    {
        [Inject] private IUiSystem _uiSystem = null;

        private SaveLoadUI _view = null;
        private List<SaveSlotItemUI> _slots = null;
        private CommandList _commandList = new CommandList();
        private CommandList.CommandIndexedValue _cmdIdxVal;

        /// <summary>
        /// GeneralUi.SaveLoadView(既にシーンに存在するUI)への参照を取得します(一度だけ呼び出してください)。
        /// </summary>
        public void Init()
        {
            _view = _uiSystem.GeneralUi.SaveLoadView;

            _slots = new List<SaveSlotItemUI> { _view.AutoSaveSlot };
            _slots.AddRange( _view.ManualSaveSlots );
        }

        /// <summary>
        /// 画面を表示します。タイトルにはセーブ画面/ロード画面の別を渡してください(例: "SAVE" / "LOAD")。
        /// </summary>
        public void Show( string title )
        {
            _view.SetTitle( title );

            var indices = new List<int>();
            for ( int i = 0; i < _slots.Count; ++i ) { indices.Add( i ); }

            _cmdIdxVal = new CommandList.CommandIndexedValue( 0, 0 );
            _commandList.Init( ref indices, CommandList.CommandDirection.VERTICAL, false, _cmdIdxVal );

            _view.gameObject.SetActive( true );
            RefreshSelection();
        }

        /// <summary>
        /// 画面を閉じます。
        /// </summary>
        public void Hide()
        {
            ClearSelection();
            _view.gameObject.SetActive( false );
        }

        /// <summary>
        /// カーソルを移動します。移動できた場合はtrueを返します。
        /// </summary>
        public bool MoveSelection( Direction dir )
        {
            if ( !_commandList.OperateListCursor( dir ) ) return false;

            RefreshSelection();

            return true;
        }

        private void RefreshSelection()
        {
            for ( int i = 0; i < _slots.Count; ++i )
            {
                _slots[i].SetSelected( i == _cmdIdxVal.index );
            }
        }

        private void ClearSelection()
        {
            foreach ( var slot in _slots ) { slot.SetSelected( false ); }
        }
    }
}
