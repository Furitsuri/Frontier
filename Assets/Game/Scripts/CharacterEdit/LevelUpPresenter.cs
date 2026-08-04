using Frontier.UI;
using System.Collections.Generic;
using Zenject;

namespace Frontier.CharacterEdit
{
    /// <summary>
    /// レベルアップ画面のViewへの窓口となるPresenter。
    /// 割り振り状態の実データ(仮のレベル・ポイント・能力値)はLevelUpContext(LevelUpHandlerが所有)が持ち、
    /// このクラスはメニューカーソル(MaxHP/Atk/Def/OKの4項目、その場限りの添字)をCommandListで管理しつつ、
    /// 画面のどこに何を表示するかの判断(不足時の赤色表示等)を行う。Viewは指示された内容を
    /// そのまま適用するだけの薄い層とする。
    /// </summary>
    public class LevelUpPresenter
    {
        private enum Row
        {
            MaxHP = 0,
            Atk,
            Def,
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
        /// 画面を表示し、メニューカーソルを先頭項目(MaxHP)にリセットします。
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
        /// 選択中の能力値に1ポイント割り振ります(OK選択中は何もしません)。
        /// </summary>
        public bool Increase()
        {
            if ( IsOkSelected() ) return false;
            if ( !_context.Increase( ToStatKind( ( Row ) _cmdIdxVal.value ) ) ) return false;

            RefreshAll();

            return true;
        }

        /// <summary>
        /// 選択中の能力値への割り振りを1ポイント取り消します(OK選択中は何もしません)。
        /// </summary>
        public bool Decrease()
        {
            if ( IsOkSelected() ) return false;
            if ( !_context.Decrease( ToStatKind( ( Row ) _cmdIdxVal.value ) ) ) return false;

            RefreshAll();

            return true;
        }

        private static LevelUpContext.StatKind ToStatKind( Row row )
        {
            switch ( row )
            {
                case Row.MaxHP: return LevelUpContext.StatKind.MaxHP;
                case Row.Atk:   return LevelUpContext.StatKind.Atk;
                case Row.Def:   return LevelUpContext.StatKind.Def;
                default:        return LevelUpContext.StatKind.MaxHP;
            }
        }

        private void RefreshAll()
        {
            var status = _context.Character.GetStatusRef;

            _view.SetLevelValues( _context.OriginalLevel, _context.TentativeLevel );
            _view.SetExpValues( _context.OriginalExp, _context.TentativeExp );

            int cost = _context.GetNextLevelCost();
            _view.SetRequiredCost( cost, _context.TentativeExp < cost );

            _view.SetMaxHpValues( status.MaxHP, _context.GetTentativeStatValue( LevelUpContext.StatKind.MaxHP ) );
            _view.SetAtkValues( status.Atk, _context.GetTentativeStatValue( LevelUpContext.StatKind.Atk ) );
            _view.SetDefValues( status.Def, _context.GetTentativeStatValue( LevelUpContext.StatKind.Def ) );
        }
    }
}
