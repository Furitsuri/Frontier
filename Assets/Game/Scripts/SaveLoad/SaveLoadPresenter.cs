using Frontier.UI;
using System.Collections.Generic;
using Zenject;
using static Constants;

namespace Frontier.SaveLoad
{
    /// <summary>
    /// セーブ/ロード画面(SaveLoadUI)の表示・選択状態を管理するPresenter。
    /// SaveLoadHandler は本クラスを介してのみ SaveLoadUI を操作する。
    /// 実際のセーブ・ロード処理(ファイルI/O)は ISlotSaveHandler{UserSaveData} に委譲する。
    /// </summary>
    public class SaveLoadPresenter
    {
        [Inject] private IUiSystem _uiSystem = null;
        [Inject] private ISlotSaveHandler<UserSaveData> _saveHdlr = null;

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
            RefreshSlotContents();
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

        /// <summary>
        /// 現在選択中のスロットのインデックスを返します。
        /// </summary>
        public int GetSelectedSlotIndex() => _cmdIdxVal.index;

        /// <summary>
        /// 各スロットの表示内容(ステージ・日時)を、実際のセーブデータの有無に応じて更新します。
        /// 保存後の再描画のため、Handler側からも呼び出せるようpublicにしています。
        /// </summary>
        public void RefreshSlotContents()
        {
            for ( int i = 0; i < _slots.Count; ++i )
            {
                var data = _saveHdlr.Load( i );
                if ( data == null )
                {
                    _slots[i].SetStageText( "NO DATA" );
                    _slots[i].SetDateText( "" );
                    continue;
                }

                _slots[i].SetStageText( $"STAGE {data.StageLevel + 1}" );
                _slots[i].SetDateText( data.SavedAt );
            }
        }
    }
}
