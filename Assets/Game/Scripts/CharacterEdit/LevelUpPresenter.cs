using Frontier.UI;
using System.Collections.Generic;
using Zenject;

namespace Frontier.CharacterEdit
{
    /// <summary>
    /// レベルアップ画面のViewへの窓口となるPresenter。
    /// 割り振り状態の実データ(仮のレベル・アニマ・StatusPoint)はLevelUpContext(LevelUpHandlerが所有)が持ち、
    /// このクラスはメニューカーソル(LEVEL/OKの2項目、その場限りの添字)をCommandListで管理しつつ、
    /// 画面のどこに何を表示するかの判断(不足時の赤色表示等)を行う。Viewは指示された内容を
    /// そのまま適用するだけの薄い層とする。
    /// </summary>
    public class LevelUpPresenter
    {
        private enum Row
        {
            Level = 0,
            OK,

            NUM,
        }

        private readonly LevelUpUI _view;
        private LevelUpContext _context;
        private readonly CommandList _commandList = new CommandList();
        private CommandList.CommandIndexedValue _cmdIdxVal;

        [Inject]
        public LevelUpPresenter( LevelUpUI view )
        {
            _view = view;
        }

        /// <summary>
        /// 画面を表示し、メニューカーソルを先頭項目(LEVEL)にリセットします。
        /// </summary>
        public void Show( LevelUpContext context )
        {
            _context = context;

            var indices = new List<int>();
            for ( int i = 0; i < ( int ) Row.NUM; ++i ) { indices.Add( i ); }

            _cmdIdxVal = new CommandList.CommandIndexedValue( 0, 0 );
            _commandList.Init( ref indices, CommandList.CommandDirection.VERTICAL, false, _cmdIdxVal );

            _view.SetSelectedRow( _cmdIdxVal.index );
            RefreshAll();
            _view.Show();
        }

        public void Hide() => _view.Hide();

        public bool MoveSelection( Direction dir )
        {
            if ( !_commandList.OperateListCursor( dir ) ) return false;

            _view.SetSelectedRow( _cmdIdxVal.index );

            return true;
        }

        public bool IsOkSelected() => ( Row ) _cmdIdxVal.value == Row.OK;

        /// <summary>
        /// 仮レベルを1つ上げます(OK選択中は何もしません)。
        /// </summary>
        public bool Increase()
        {
            if ( IsOkSelected() ) return false;
            if ( !_context.Increase() ) return false;

            RefreshAll();

            return true;
        }

        /// <summary>
        /// 仮レベルを1つ下げます(OK選択中は何もしません)。
        /// </summary>
        public bool Decrease()
        {
            if ( IsOkSelected() ) return false;
            if ( !_context.Decrease() ) return false;

            RefreshAll();

            return true;
        }

        private void RefreshAll()
        {
            _view.SetLevelValues( _context.OriginalLevel, _context.TentativeLevel );
            _view.SetAnimaValues( _context.OriginalAnima, _context.TentativeAnima );

            int cost = _context.GetNextLevelCost();
            _view.SetRequiredCost( cost, _context.TentativeAnima < cost );

            _view.SetStatusPointValues( _context.OriginalStatusPoint, _context.TentativeStatusPoint );
        }
    }
}
