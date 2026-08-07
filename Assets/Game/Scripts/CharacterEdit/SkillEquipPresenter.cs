using Frontier.Combat;
using Frontier.UI;
using System.Collections.Generic;
using Zenject;
using static Constants;

namespace Frontier.CharacterEdit
{
    /// <summary>
    /// 装備スキル設定画面のViewへの窓口となるPresenter。
    /// 左側(装備枠+OK)・右側(「スキルを外す」+所持スキル一覧)の2つのCommandListを切り替えながら管理し、
    /// どちらのウィンドウにカーソルがあるか、右側では現在どの枠を編集中かを保持する。
    /// 割り振りの実データ(仮の装備構成・所持数)はSkillEquipContext(SkillEquipHandlerが所有)が持ち、
    /// このクラスは画面のどこに何を表示するかの判断(在庫切れ・編集中枠と同一スキルのグレー表示・
    /// フォーカス中スキルの説明文切り替え等)を行う。Viewは指示された内容をそのまま適用するだけの
    /// 薄い層とする。
    /// </summary>
    public class SkillEquipPresenter
    {
        private enum Pane
        {
            Left = 0,
            Right,
        }

        private const int OK_ROW = EQUIPABLE_SKILL_MAX_NUM;

        // 右側CommandListの値0は常に「スキルを外す」(一番上に固定)、1以降が所持スキル一覧に対応する
        private const int REMOVE_ROW_VALUE = 0;

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
        /// フォーカス表示(スキルUIの外枠強調)とスキル説明文はこの時点から表示します。
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
            _view.SetSelectedSlot( _leftCmdIdxVal.index );
            RefreshRightList();
            _view.SetRightSelectedRow( -2 );
            RefreshExplanation();
            _view.Show();
        }

        /// <summary>
        /// 入力受付を伴わないプレビュー表示です。まだ画面へ遷移していない状態のため、
        /// カーソルのフォーカス表示(拡大・外枠)やスキル説明文は表示しません。
        /// </summary>
        public void ShowPreview( SkillEquipContext context )
        {
            _context = context;
            _currentPane = Pane.Left;
            _editingSlotIndex = -1;

            RefreshLeftSlots();
            _view.SetSelectedSlot( -1 );
            RefreshRightList();
            _view.SetRightSelectedRow( -2 );
            _view.HideExplanation();
            _view.Show();
        }

        public void Hide() => _view.Hide();

        public bool IsLeftPane => _currentPane == Pane.Left;

        public bool MoveSelection( Direction dir )
        {
            bool moved;
            if ( IsLeftPane )
            {
                moved = _leftCommandList.OperateListCursor( dir );
                if ( moved ) { _view.SetSelectedSlot( _leftCmdIdxVal.index ); }
            }
            else
            {
                moved = _rightCommandList.OperateListCursor( dir );
                if ( moved ) { RefreshRightSelectionDisplay(); }
            }

            if ( moved ) { RefreshExplanation(); }

            return moved;
        }

        public bool IsOkSelected() => IsLeftPane && _leftCmdIdxVal.value == OK_ROW;

        /// <summary>
        /// 左側で選択中の枠を右側ウィンドウでの編集対象にし、カーソルを右側へ遷移させます。
        /// 右側には常に「スキルを外す」が先頭にあるため、OKが選択されている場合を除き必ず遷移できます。
        /// </summary>
        public bool EnterRightPane()
        {
            if ( !IsLeftPane ) return false;
            if ( IsOkSelected() ) return false;

            _editingSlotIndex = _leftCmdIdxVal.value;
            RefreshRightList();

            _currentPane = Pane.Right;

            // 値0:スキルを外す、値1〜:所持スキル一覧(要素数は「スキルを外す」の分だけ+1)
            var indices = new List<int>();
            for ( int i = 0; i <= _rightSkillIds.Count; ++i ) { indices.Add( i ); }

            _rightCmdIdxVal = new CommandList.CommandIndexedValue( 0, 0 );
            _rightCommandList.Init( ref indices, CommandList.CommandDirection.VERTICAL, false, _rightCmdIdxVal );

            // 左側は編集対象の枠のみ通常表示のまま、他をグレーアウトして「どの枠を編集中か」を示す
            _view.SetLockedSlot( _editingSlotIndex );
            RefreshRightSelectionDisplay();
            RefreshExplanation();

            return true;
        }

        /// <summary>
        /// 右側ウィンドウでの選択を確定せずに左側ウィンドウへ戻ります(右側でのキャンセル操作)。
        /// </summary>
        public void ExitRightPane()
        {
            _currentPane = Pane.Left;
            _editingSlotIndex = -1;

            _view.SetRightSelectedRow( -2 );
            _view.SetSelectedSlot( _leftCmdIdxVal.index );
            RefreshRightList();
            RefreshExplanation();
        }

        /// <summary>
        /// 右側ウィンドウで選択中の項目を確定します。「スキルを外す」が選択されていれば編集中の枠を
        /// 空にし、所持スキルが選択されていればそのスキルを編集中の枠へ装備します。選択不可(グレー表示)
        /// の項目や在庫が無い場合は何もせずfalseを返します。成功した場合は左側ウィンドウへ戻ります。
        /// </summary>
        public bool ConfirmRightSelection()
        {
            if ( IsLeftPane ) return false;

            if ( _rightCmdIdxVal.value == REMOVE_ROW_VALUE )
            {
                if ( IsRemoveRowUnavailable() ) return false;
                if ( !_context.UnequipSkill( _editingSlotIndex ) ) return false;

                RefreshLeftSlots();
                ExitRightPane();

                return true;
            }

            int skillIndex = _rightCmdIdxVal.value - 1;
            if ( skillIndex < 0 || _rightSkillIds.Count <= skillIndex ) return false;

            var skillID = _rightSkillIds[skillIndex];
            if ( IsRowUnavailable( skillID ) ) return false;
            if ( !_context.EquipSkill( _editingSlotIndex, skillID ) ) return false;

            RefreshLeftSlots();
            ExitRightPane();

            return true;
        }

        /// <summary>
        /// 「スキルを外す」が選択不可(グレー表示)かどうかを判定します。
        /// 編集中の枠が元々未装備の場合は、外す対象が無いため選択不可にします。
        /// </summary>
        private bool IsRemoveRowUnavailable()
        {
            if ( _editingSlotIndex < 0 ) return true;

            return !SkillsData.IsValidSkill( _context.GetEquippedSkill( _editingSlotIndex ) );
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

            _view.SetRemoveRowUnavailable( IsRemoveRowUnavailable() );

            _view.SetInventoryRowCount( _rightSkillIds.Count );
            for ( int i = 0; i < _rightSkillIds.Count; ++i )
            {
                var skillID = _rightSkillIds[i];
                var skillData = SkillsData.data[( int ) skillID];
                int count = _context.GetTentativeCount( skillID );
                _view.SetInventoryRow( i, skillData.Name, skillData.SituationType, count, IsRowUnavailable( skillID ) );
            }
        }

        private void RefreshRightSelectionDisplay()
        {
            int value = _rightCmdIdxVal.value;
            _view.SetRightSelectedRow( value == REMOVE_ROW_VALUE ? -1 : value - 1 );
        }

        /// <summary>
        /// 現在フォーカスしているスキルの説明文表示を更新します(左側:選択中の枠に装備されている
        /// スキル、右側:選択中の一覧行のスキル)。OK・「スキルを外す」・未装備の枠にフォーカスしている
        /// 場合は非表示にします。
        /// </summary>
        private void RefreshExplanation()
        {
            if ( IsLeftPane )
            {
                if ( IsOkSelected() ) { _view.HideExplanation(); return; }

                _view.SetExplanation( _context.GetEquippedSkill( _leftCmdIdxVal.value ) );
                return;
            }

            if ( _rightCmdIdxVal.value == REMOVE_ROW_VALUE ) { _view.HideExplanation(); return; }

            int skillIndex = _rightCmdIdxVal.value - 1;
            if ( skillIndex < 0 || _rightSkillIds.Count <= skillIndex ) { _view.HideExplanation(); return; }

            _view.SetExplanation( _rightSkillIds[skillIndex] );
        }
    }
}
