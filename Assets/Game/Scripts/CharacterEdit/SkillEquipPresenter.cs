using Frontier.Combat;
using Frontier.UI;
using System.Collections.Generic;
using Zenject;
using static Constants;

namespace Frontier.CharacterEdit
{
    /// <summary>
    /// 装備スキル設定画面のViewへの窓口となるPresenter。
    /// 左側(アクティブ装備枠+パッシブ装備枠+OK)・右側(「スキルを外す」+所持スキル一覧)の2つのCommandListを切り替えながら管理し、
    /// どちらのウィンドウにカーソルがあるか、右側では現在どの枠を編集中かを保持する。
    /// 割り振りの実データ(仮の装備構成・所持数)はSkillEquipContext(SkillEquipHandlerが所有)が持ち、
    /// このクラスは画面のどこに何を表示するかの判断(在庫切れ・編集中枠と同一スキルのグレー表示・
    /// フォーカス中スキルの説明文切り替え等)を行う。Viewは指示された内容をそのまま適用するだけの
    /// 薄い層とする。
    /// 左側の行番号は「アクティブ枠(0〜EQUIPABLE_ACTIVE_SKILL_MAX_NUM-1) → パッシブ枠 → OK」の順に並ぶ
    /// (SkillEquipUIの左側の行番号と一致させている)。右側の所持スキル一覧は、編集中の枠の種類に
    /// 装備可能なスキルのみに絞り込んで表示する。
    /// </summary>
    public class SkillEquipPresenter
    {
        private enum Pane
        {
            Left = 0,
            Right,
        }

        private const int PASSIVE_FIRST_ROW = EQUIPABLE_ACTIVE_SKILL_MAX_NUM;
        private const int OK_ROW            = EQUIPABLE_ACTIVE_SKILL_MAX_NUM + EQUIPABLE_PASSIVE_SKILL_MAX_NUM;

        // 右側CommandListの値0は常に「スキルを外す」(一番上に固定)、1以降が所持スキル一覧に対応する
        private const int REMOVE_ROW_VALUE = 0;

        private readonly SkillEquipUI _view;
        private SkillEquipContext _context;
        private Pane _currentPane;

        // 右側ウィンドウで編集対象にしている左側の行番号(-1:右側ウィンドウ未使用中)
        private int _editingRow = -1;

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
            _editingRow = -1;

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
            _editingRow = -1;

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

            _editingRow = _leftCmdIdxVal.value;
            RefreshRightList();

            _currentPane = Pane.Right;

            // 値0:スキルを外す、値1〜:所持スキル一覧(要素数は「スキルを外す」の分だけ+1)
            var indices = new List<int>();
            for ( int i = 0; i <= _rightSkillIds.Count; ++i ) { indices.Add( i ); }

            _rightCmdIdxVal = new CommandList.CommandIndexedValue( 0, 0 );
            _rightCommandList.Init( ref indices, CommandList.CommandDirection.VERTICAL, false, _rightCmdIdxVal );

            // 左側は編集対象の枠のみ通常表示のまま、他をグレーアウトして「どの枠を編集中か」を示す
            _view.SetLockedSlot( _editingRow );
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
            _editingRow = -1;

            _view.SetRightSelectedRow( -2 );
            _view.SetSelectedSlot( _leftCmdIdxVal.index );
            RefreshRightList();
            RefreshExplanation();
        }

        /// <summary>
        /// 右側ウィンドウで現在フォーカスしている項目を確定できるかどうかを判定します。
        /// SkillEquipHandler.CanAcceptConfirm()が、右側ウィンドウにいる間CONFIRM入力そのものを
        /// 受け付けるべきかの判断に使用するほか、ConfirmRightSelection()自身もこれを内部で使い、
        /// 「選択不可の項目は確定できない」条件を一箇所にまとめている。
        /// </summary>
        public bool CanConfirmRightSelection()
        {
            if ( IsLeftPane ) return false;

            if ( _rightCmdIdxVal.value == REMOVE_ROW_VALUE ) return !IsRemoveRowUnavailable();

            int skillIndex = _rightCmdIdxVal.value - 1;
            if ( skillIndex < 0 || _rightSkillIds.Count <= skillIndex ) return false;

            return !IsRowUnavailable( _rightSkillIds[skillIndex] );
        }

        /// <summary>
        /// 右側ウィンドウで選択中の項目を確定します。「スキルを外す」が選択されていれば編集中の枠を
        /// 空にし、所持スキルが選択されていればそのスキルを編集中の枠へ装備します。選択不可(グレー表示)
        /// の項目や在庫が無い場合は何もせずfalseを返します(CanConfirmRightSelection()参照)。
        /// 成功した場合は左側ウィンドウへ戻ります。
        /// </summary>
        public bool ConfirmRightSelection()
        {
            if ( !CanConfirmRightSelection() ) return false;

            if ( _rightCmdIdxVal.value == REMOVE_ROW_VALUE )
            {
                if ( !TryGetSlot( _editingRow, out var removeSlotType, out var removeSlotIndex ) ) return false;
                if ( !_context.UnequipSkill( removeSlotType, removeSlotIndex ) ) return false;

                RefreshLeftSlots();
                ExitRightPane();

                return true;
            }

            var skillID = _rightSkillIds[_rightCmdIdxVal.value - 1];
            if ( !TryGetSlot( _editingRow, out var slotType, out var slotIndex ) ) return false;
            if ( !_context.EquipSkill( slotType, slotIndex, skillID ) ) return false;

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
            return !SkillsData.IsValidSkill( GetSkillAtRow( _editingRow ) );
        }

        /// <summary>
        /// 右側ウィンドウで指定スキルを選択不可(グレー表示)にすべきかどうかを判定します。
        /// 所持数が0、現在編集中の枠に既に装備されているスキル、または編集中の枠の種類に装備出来ない
        /// スキルの場合はtrueを返します(一覧は枠の種類で絞り込み済みのため、最後の条件は保険です)。
        /// </summary>
        private bool IsRowUnavailable( SkillID skillID )
        {
            if ( _context.GetTentativeCount( skillID ) <= 0 ) return true;
            if ( TryGetSlot( _editingRow, out var slotType, out var slotIndex ) )
            {
                if ( skillID == _context.GetEquippedSkill( slotType, slotIndex ) ) return true;
                if ( !SkillsData.IsSkillMatchSlotType( skillID, slotType ) ) return true;
            }

            return false;
        }

        private void RefreshLeftSlots()
        {
            for ( int row = 0; row < OK_ROW; ++row )
            {
                _view.SetSlotSkill( row, GetSkillAtRow( row ) );
            }
        }

        private void RefreshRightList()
        {
            // 編集中は枠の種類に装備可能なスキルのみ、それ以外(左側選択中・プレビュー)は所持スキル全てを表示する
            _rightSkillIds = TryGetSlot( _editingRow, out var slotType, out _ )
                ? new List<SkillID>( _context.GetOwnedSkillIdsOrdered( slotType ) )
                : new List<SkillID>( _context.GetOwnedSkillIdsOrdered() );

            _view.SetRemoveRowUnavailable( IsRemoveRowUnavailable() );

            _view.SetInventoryRowCount( _rightSkillIds.Count );
            for ( int i = 0; i < _rightSkillIds.Count; ++i )
            {
                var skillID = _rightSkillIds[i];
                int count = _context.GetTentativeCount( skillID );
                _view.SetInventoryRow( i, skillID, count, IsRowUnavailable( skillID ) );
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

                _view.SetExplanation( GetSkillAtRow( _leftCmdIdxVal.value ) );
                return;
            }

            if ( _rightCmdIdxVal.value == REMOVE_ROW_VALUE ) { _view.HideExplanation(); return; }

            int skillIndex = _rightCmdIdxVal.value - 1;
            if ( skillIndex < 0 || _rightSkillIds.Count <= skillIndex ) { _view.HideExplanation(); return; }

            _view.SetExplanation( _rightSkillIds[skillIndex] );
        }

        /// <summary>
        /// 左側の行番号を、装備枠の種類と種類内での枠番号へ変換します。OK行・範囲外の場合はfalseを返します。
        /// </summary>
        private static bool TryGetSlot( int row, out SkillSlotType slotType, out int slotIndex )
        {
            slotType  = SkillSlotType.ACTIVE;
            slotIndex = -1;

            if ( row < 0 || OK_ROW <= row ) return false;

            if ( row < PASSIVE_FIRST_ROW )
            {
                slotIndex = row;
                return true;
            }

            slotType  = SkillSlotType.PASSIVE;
            slotIndex = row - PASSIVE_FIRST_ROW;
            return true;
        }

        /// <summary>
        /// 左側の指定行の枠に仮に装備されているスキルを返します(OK行・範囲外の場合はSkillID.NONE)。
        /// </summary>
        private SkillID GetSkillAtRow( int row )
        {
            if ( !TryGetSlot( row, out var slotType, out var slotIndex ) ) return SkillID.NONE;

            return _context.GetEquippedSkill( slotType, slotIndex );
        }
    }
}
