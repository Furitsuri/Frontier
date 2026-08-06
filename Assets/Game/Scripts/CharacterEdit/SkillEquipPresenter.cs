using Frontier.Combat;
using Frontier.UI;
using System.Collections.Generic;
using Zenject;
using static Constants;

namespace Frontier.CharacterEdit
{
    /// <summary>
    /// 装備スキル設定画面のViewへの窓口となるPresenter。
    /// 左側(装備枠+OK)・右側(所持スキル一覧)の2つのCommandListを切り替えながら管理し、
    /// どちらのウィンドウにカーソルがあるか、右側では現在どの枠を編集中かを保持する。
    /// 割り振りの実データ(仮の装備構成・所持数)はSkillEquipContext(SkillEquipHandlerが所有)が持ち、
    /// このクラスは画面のどこに何を表示するかの判断(在庫切れ・編集中枠と同一スキルのグレー表示等)を行う。
    /// Viewは指示された内容をそのまま適用するだけの薄い層とする。
    /// </summary>
    public class SkillEquipPresenter
    {
        private enum Pane
        {
            Left = 0,
            Right,
        }

        private const int OK_ROW = EQUIPABLE_SKILL_MAX_NUM;

        private readonly SkillEquipUI _view;
        private SkillEquipContext _context;
        private Pane _currentPane;

        // 右側ウィンドウで編集対象にしている左側の枠番号(-1:右側ウィンドウ未使用中)
        private int _editingSlotIndex = -1;

        private readonly CommandList _leftCommandList = new CommandList();
        private CommandList.CommandIndexedValue _leftCmdIdxVal;

        private readonly CommandList _rightCommandList = new CommandList();
        private CommandList.CommandIndexedValue _rightCmdIdxVal;
        private List<SkillID> _rightSkillIds = new List<SkillID>();

        [Inject]
        public SkillEquipPresenter( SkillEquipUI view )
        {
            _view = view;
        }

        /// <summary>
        /// 画面を表示し、カーソルを左側ウィンドウの先頭枠にリセットします。
        /// </summary>
        public void Show( SkillEquipContext context )
        {
            _context = context;
            _currentPane = Pane.Left;
            _editingSlotIndex = -1;

            var indices = new List<int>();
            for ( int i = 0; i <= OK_ROW; ++i ) { indices.Add( i ); }

            _leftCmdIdxVal = new CommandList.CommandIndexedValue( 0, 0 );
            _leftCommandList.Init( ref indices, CommandList.CommandDirection.VERTICAL, false, _leftCmdIdxVal );

            RefreshLeftSlots();
            _view.SetLeftSelectedRow( _leftCmdIdxVal.index );
            RefreshRightList();
            _view.SetInventorySelectedRow( -1 );
            _view.Show();
        }

        public void Hide() => _view.Hide();

        public bool IsLeftPane => _currentPane == Pane.Left;

        public bool MoveSelection( Direction dir )
        {
            if ( IsLeftPane )
            {
                if ( !_leftCommandList.OperateListCursor( dir ) ) return false;

                _view.SetLeftSelectedRow( _leftCmdIdxVal.index );
                return true;
            }

            if ( _rightSkillIds.Count == 0 ) return false;
            if ( !_rightCommandList.OperateListCursor( dir ) ) return false;

            _view.SetInventorySelectedRow( _rightCmdIdxVal.index );
            return true;
        }

        public bool IsOkSelected() => IsLeftPane && _leftCmdIdxVal.value == OK_ROW;

        /// <summary>
        /// 左側で選択中の枠を右側ウィンドウでの編集対象にし、カーソルを右側へ遷移させます。
        /// OKが選択されている場合や、所持したことのあるスキルが1つもない場合は何もせずfalseを返します。
        /// </summary>
        public bool EnterRightPane()
        {
            if ( !IsLeftPane ) return false;
            if ( IsOkSelected() ) return false;

            _editingSlotIndex = _leftCmdIdxVal.value;
            RefreshRightList();

            if ( _rightSkillIds.Count == 0 )
            {
                _editingSlotIndex = -1;
                return false;
            }

            _currentPane = Pane.Right;

            var indices = new List<int>();
            for ( int i = 0; i < _rightSkillIds.Count; ++i ) { indices.Add( i ); }

            _rightCmdIdxVal = new CommandList.CommandIndexedValue( 0, 0 );
            _rightCommandList.Init( ref indices, CommandList.CommandDirection.VERTICAL, false, _rightCmdIdxVal );

            _view.SetLeftSelectedRow( -1 );
            _view.SetInventorySelectedRow( _rightCmdIdxVal.index );

            return true;
        }

        /// <summary>
        /// 右側ウィンドウでの選択を確定せずに左側ウィンドウへ戻ります(右側でのキャンセル操作)。
        /// </summary>
        public void ExitRightPane()
        {
            _currentPane = Pane.Left;
            _editingSlotIndex = -1;

            _view.SetInventorySelectedRow( -1 );
            _view.SetLeftSelectedRow( _leftCmdIdxVal.index );
            RefreshRightList();
        }

        /// <summary>
        /// 右側ウィンドウで選択中のスキルを、左側で編集中だった枠へ装備します。
        /// 在庫が無い、または編集中の枠に既に装備されている(選択の意味がない)場合は何もせずfalseを返します。
        /// 成功した場合は両ウィンドウの表示を更新した上で左側ウィンドウへ戻ります。
        /// </summary>
        public bool ConfirmRightSelection()
        {
            if ( IsLeftPane ) return false;
            if ( _rightSkillIds.Count == 0 ) return false;

            var skillID = _rightSkillIds[_rightCmdIdxVal.value];
            if ( IsRowUnavailable( skillID ) ) return false;
            if ( !_context.EquipSkill( _editingSlotIndex, skillID ) ) return false;

            RefreshLeftSlots();
            ExitRightPane();

            return true;
        }

        /// <summary>
        /// 右側ウィンドウで指定スキルを選択不可(グレー表示)にすべきかどうかを判定します。
        /// 所持数が0、または現在編集中の枠に既に装備されているスキルの場合はtrueを返します。
        /// </summary>
        private bool IsRowUnavailable( SkillID skillID )
        {
            if ( _context.GetTentativeCount( skillID ) <= 0 ) return true;
            if ( _editingSlotIndex >= 0 && skillID == _context.GetEquippedSkill( _editingSlotIndex ) ) return true;

            return false;
        }

        private void RefreshLeftSlots()
        {
            for ( int i = 0; i < EQUIPABLE_SKILL_MAX_NUM; ++i )
            {
                _view.SetSlotSkill( i, _context.GetEquippedSkill( i ) );
            }
        }

        private void RefreshRightList()
        {
            _rightSkillIds = new List<SkillID>( _context.GetOwnedSkillIdsOrdered() );

            _view.SetInventoryRowCount( _rightSkillIds.Count );
            for ( int i = 0; i < _rightSkillIds.Count; ++i )
            {
                var skillID = _rightSkillIds[i];
                var skillData = SkillsData.data[( int ) skillID];
                int count = _context.GetTentativeCount( skillID );
                _view.SetInventoryRow( i, skillData.Name, skillData.SituationType, count, IsRowUnavailable( skillID ) );
            }
        }
    }
}
