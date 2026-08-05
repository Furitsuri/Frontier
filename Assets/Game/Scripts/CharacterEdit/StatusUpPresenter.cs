using Frontier.UI;
using System.Collections.Generic;
using Zenject;

namespace Frontier.CharacterEdit
{
    /// <summary>
    /// ステータス上昇画面のViewへの窓口となるPresenter。
    /// 割り振り状態の実データ(仮のStatusPoint・能力値)はStatusUpContext(StatusUpHandlerが所有)が持ち、
    /// このクラスはメニューカーソル(MaxHP/Atk/Def/MoveRange/JumpForce/MaxActionGauge/RecoveryActionGauge/OKの
    /// 8項目、その場限りの添字)をCommandListで管理しつつ、画面のどこに何を表示するかの判断
    /// (不足時の赤色表示等)を行う。Viewは指示された内容をそのまま適用するだけの薄い層とする。
    /// </summary>
    public class StatusUpPresenter
    {
        private enum Row
        {
            MaxHP = 0,
            Atk,
            Def,
            MoveRange,
            JumpForce,
            MaxActionGauge,
            RecoveryActionGauge,
            OK,

            NUM,
        }

        private readonly StatusUpUI _view;
        private StatusUpContext _context;
        private readonly CommandList _commandList = new CommandList();
        private CommandList.CommandIndexedValue _cmdIdxVal;

        [Inject]
        public StatusUpPresenter( StatusUpUI view )
        {
            _view = view;
        }

        /// <summary>
        /// 画面を表示し、メニューカーソルを先頭項目(MaxHP)にリセットします。
        /// </summary>
        public void Show( StatusUpContext context )
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

        private static StatusUpContext.StatKind ToStatKind( Row row )
        {
            switch ( row )
            {
                case Row.MaxHP:          return StatusUpContext.StatKind.MaxHP;
                case Row.Atk:            return StatusUpContext.StatKind.Atk;
                case Row.Def:            return StatusUpContext.StatKind.Def;
                case Row.MoveRange:      return StatusUpContext.StatKind.MoveRange;
                case Row.JumpForce:      return StatusUpContext.StatKind.JumpForce;
                case Row.MaxActionGauge:      return StatusUpContext.StatKind.MaxActionGauge;
                case Row.RecoveryActionGauge: return StatusUpContext.StatKind.RecoveryActionGauge;
                default:                      return StatusUpContext.StatKind.MaxHP;
            }
        }

        private void RefreshAll()
        {
            var status = _context.Character.GetStatusRef;

            _view.SetSpValues( _context.OriginalStatusPoint, _context.TentativeStatusPoint );

            RefreshRow( status, StatusUpContext.StatKind.MaxHP,          _view.SetMaxHpValues );
            RefreshRow( status, StatusUpContext.StatKind.Atk,            _view.SetAtkValues );
            RefreshRow( status, StatusUpContext.StatKind.Def,            _view.SetDefValues );
            RefreshRow( status, StatusUpContext.StatKind.MoveRange,      _view.SetMoveRangeValues );
            RefreshRow( status, StatusUpContext.StatKind.JumpForce,      _view.SetJumpForceValues );
            RefreshRow( status, StatusUpContext.StatKind.MaxActionGauge,      _view.SetMaxActionGaugeValues );
            RefreshRow( status, StatusUpContext.StatKind.RecoveryActionGauge, _view.SetRecoveryActionGaugeValues );
        }

        private delegate void SetRowValues( int current, int tentative, int cost, bool insufficient, bool isMaxed );

        private void RefreshRow( in Frontier.Entities.Status status, StatusUpContext.StatKind stat, SetRowValues setter )
        {
            int cost = StatusUpContext.GetCost( stat );
            int tentativeValue = _context.GetTentativeStatValue( stat );
            bool isMaxed = StatusUpContext.GetMax( stat ) <= tentativeValue;

            setter( StatusUpContext.GetBaseStatValue( status, stat ), tentativeValue, cost, _context.TentativeStatusPoint < cost, isMaxed );
        }
    }
}
