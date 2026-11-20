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
    /// (SkillEquipUIの左側の行番号と一致させている)。右側の所持スキル一覧は、左側でフォーカス中(右側の
    /// 操作中は編集中)の枠の種類のスキルを表示する。
    /// 所持スキルがViewの表示行数(SkillEquipUI.VisibleInventoryRowCount)より多い場合は、カーソルに追従して
    /// 表示範囲をスクロールする(どの範囲を表示するかはこのクラスが決め、Viewには表示行単位で内容を渡す)。
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
        private readonly ILocalizationService _localization;
        private SkillEquipContext _context;
        private Pane _currentPane;

        // 右側ウィンドウで編集対象にしている左側の行番号(-1:右側ウィンドウ未使用中)
        private int _editingRow = -1;

        private readonly CommandList _leftCommandList = new CommandList();
        private CommandList.CommandIndexedValue _leftCmdIdxVal;

        private readonly CommandList _rightCommandList = new CommandList();
        private CommandList.CommandIndexedValue _rightCmdIdxVal;

        // 右側の所持スキル一覧に表示中のスキルの種類と、その一覧(表示中の種類の方のみ使用する)
        private SkillSlotType _rightListType = SkillSlotType.ACTIVE;
        private List<ActiveSkillID> _rightActiveIds = new List<ActiveSkillID>();
        private List<PassiveSkillID> _rightPassiveIds = new List<PassiveSkillID>();

        // 右側の所持スキル一覧で、表示範囲の先頭にある一覧のindex(スクロール位置)
        private int _rightScrollTop = 0;

        // アクションゲージ残量表示の目盛りごとの状態(使い回し用)
        private readonly List<SkillEquipUI.ActionGaugePipState> _pipStates = new List<SkillEquipUI.ActionGaugePipState>( ACTION_GAUGE_MAX );

        [Inject]
        public SkillEquipPresenter( SkillEquipUI view, ILocalizationService localization )
        {
            _view           = view;
            _localization   = localization;
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
            _rightListType = SkillSlotType.ACTIVE;
            _rightScrollTop = 0;

            var indices = new List<int>();
            for ( int i = 0; i <= OK_ROW; ++i ) { indices.Add( i ); }

            _leftCmdIdxVal = new CommandList.CommandIndexedValue( 0, 0 );
            _leftCommandList.Init( ref indices, CommandList.CommandDirection.VERTICAL, false, _leftCmdIdxVal );

            RefreshLeftSlots();
            _view.SetSelectedSlot( _leftCmdIdxVal.index );
            RefreshRightList();
            _view.SetRightSelectedRow( -2 );
            RefreshFocusDisplay();
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
            _rightListType = SkillSlotType.ACTIVE;
            _rightScrollTop = 0;

            RefreshLeftSlots();
            _view.SetSelectedSlot( -1 );
            RefreshRightList();
            _view.SetRightSelectedRow( -2 );
            _view.HideExplanation();
            RefreshActionGaugeAndWarning();
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
                if ( moved )
                {
                    _view.SetSelectedSlot( _leftCmdIdxVal.index );

                    // フォーカス中の枠の種類が変わった場合は、右側の一覧もその種類の所持スキルへ切り替える
                    if ( TryGetSlot( _leftCmdIdxVal.value, out var slotType, out _ ) && slotType != _rightListType )
                    {
                        _rightListType = slotType;
                        _rightScrollTop = 0;
                        RefreshRightList();
                    }
                }
            }
            else
            {
                moved = _rightCommandList.OperateListCursor( dir );
                if ( moved )
                {
                    if ( ScrollToRightSelection() ) { RefreshRightList(); }
                    RefreshRightSelectionDisplay();
                }
            }

            if ( moved ) { RefreshFocusDisplay(); }

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
            if ( !TryGetSlot( _leftCmdIdxVal.value, out var slotType, out _ ) ) return false;

            _editingRow = _leftCmdIdxVal.value;
            _rightListType = slotType;
            _rightScrollTop = 0;
            RefreshRightList();

            _currentPane = Pane.Right;

            // 値0:スキルを外す、値1〜:所持スキル一覧(要素数は「スキルを外す」の分だけ+1)
            var indices = new List<int>();
            for ( int i = 0; i <= RightListCount; ++i ) { indices.Add( i ); }

            _rightCmdIdxVal = new CommandList.CommandIndexedValue( 0, 0 );
            _rightCommandList.Init( ref indices, CommandList.CommandDirection.VERTICAL, false, _rightCmdIdxVal );

            // 左側は編集対象の枠のみ通常表示のまま、他をグレーアウトして「どの枠を編集中か」を示す
            _view.SetLockedSlot( _editingRow );
            RefreshRightSelectionDisplay();
            RefreshFocusDisplay();

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
            _rightScrollTop = 0;
            RefreshRightList();
            RefreshFocusDisplay();
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

            int listIndex = _rightCmdIdxVal.value - 1;
            if ( listIndex < 0 || RightListCount <= listIndex ) return false;

            return !IsRightRowUnavailable( listIndex );
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
            if ( !TryGetSlot( _editingRow, out var slotType, out var slotIndex ) ) return false;

            bool succeeded;
            int destRow = _editingRow;
            if ( _rightCmdIdxVal.value == REMOVE_ROW_VALUE )
            {
                // 外した枠には後ろの枠のスキルが詰められるため、カーソルは同じ枠のままとする
                succeeded = ( slotType == SkillSlotType.PASSIVE )
                    ? _context.Passive.Unequip( slotIndex )
                    : _context.Active.Unequip( slotIndex );
            }
            else
            {
                // 空き枠への装備は先頭の空き枠に詰めて入るため、カーソルを実際に装備された枠へ合わせる
                int listIndex = _rightCmdIdxVal.value - 1;
                if ( slotType == SkillSlotType.PASSIVE )
                {
                    destRow   = PASSIVE_FIRST_ROW + _context.Passive.GetEquipDestination( slotIndex );
                    succeeded = _context.EquipPassive( slotIndex, _rightPassiveIds[listIndex] );
                }
                else
                {
                    destRow   = _context.Active.GetEquipDestination( slotIndex );
                    succeeded = _context.Active.Equip( slotIndex, _rightActiveIds[listIndex] );
                }
            }

            if ( !succeeded ) return false;

            RefreshLeftSlots();
            _leftCommandList.SetCurrentValue( destRow );
            ExitRightPane();

            return true;
        }

        private int RightListCount => ( _rightListType == SkillSlotType.PASSIVE ) ? _rightPassiveIds.Count : _rightActiveIds.Count;

        /// <summary>
        /// 「スキルを外す」が選択不可(グレー表示)かどうかを判定します。
        /// 編集中の枠が元々未装備の場合は、外す対象が無いため選択不可にします。
        /// </summary>
        private bool IsRemoveRowUnavailable()
        {
            if ( !TryGetSlot( _editingRow, out var slotType, out var slotIndex ) ) return true;

            return ( slotType == SkillSlotType.PASSIVE )
                ? !PassiveSkillsData.IsValidSkill( _context.Passive.GetEquippedSkill( slotIndex ) )
                : !ActiveSkillsData.IsValidSkill( _context.Active.GetEquippedSkill( slotIndex ) );
        }

        /// <summary>
        /// 右側の所持スキル一覧の指定行を選択不可(グレー表示)にすべきかどうかを判定します。
        /// 編集中の枠がある場合はその枠へ装備出来ないスキル(所持数0、既にその枠に装備されている、
        /// 他の枠に同一スキルを装備済み、パッシブスキルの装備コストが足りない等。SkillEquipSlotGroup.CanEquip、SkillEquipContext.CanEquipPassive参照)、
        /// 編集中でない場合は所持数が0のスキルを選択不可とします。
        /// </summary>
        private bool IsRightRowUnavailable( int listIndex )
        {
            bool isEditing = TryGetSlot( _editingRow, out _, out var slotIndex );

            if ( _rightListType == SkillSlotType.PASSIVE )
            {
                var skillID = _rightPassiveIds[listIndex];
                return isEditing ? !_context.CanEquipPassive( slotIndex, skillID ) : _context.Passive.GetTentativeCount( skillID ) <= 0;
            }
            else
            {
                var skillID = _rightActiveIds[listIndex];
                return isEditing ? !_context.Active.CanEquip( slotIndex, skillID ) : _context.Active.GetTentativeCount( skillID ) <= 0;
            }
        }

        private void RefreshLeftSlots()
        {
            for ( int i = 0; i < _context.Active.SlotCount; ++i )
            {
                _view.SetSlotSkill( i, _context.Active.GetEquippedSkill( i ) );
                // パッシブスキルの装備コストでアクションゲージ最大値が使用コストを下回る場合はグレー表示にする
                _view.SetSlotUseable( i, _context.IsActiveSlotWithinMaxActionGauge( i ) );
            }
            for ( int i = 0; i < _context.Passive.SlotCount; ++i )
            {
                _view.SetSlotSkill( PASSIVE_FIRST_ROW + i, _context.Passive.GetEquippedSkill( i ) );
                _view.SetSlotUseable( PASSIVE_FIRST_ROW + i, true );
            }
        }

        private void RefreshRightList()
        {
            _rightActiveIds  = new List<ActiveSkillID>( _context.Active.GetOwnedSkillIdsOrdered() );
            _rightPassiveIds = new List<PassiveSkillID>( _context.Passive.GetOwnedSkillIdsOrdered() );

            _view.SetRemoveRowUnavailable( IsRemoveRowUnavailable() );

            int count        = RightListCount;
            int visibleCount = _view.VisibleInventoryRowCount;
            _rightScrollTop  = UnityEngine.Mathf.Clamp( _rightScrollTop, 0, UnityEngine.Mathf.Max( 0, count - visibleCount ) );

            int shownCount = UnityEngine.Mathf.Min( visibleCount, count - _rightScrollTop );
            _view.SetInventoryRowCount( shownCount );
            for ( int row = 0; row < shownCount; ++row )
            {
                int listIndex    = _rightScrollTop + row;
                bool unavailable = IsRightRowUnavailable( listIndex );
                if ( _rightListType == SkillSlotType.PASSIVE )
                {
                    var skillID = _rightPassiveIds[listIndex];
                    _view.SetInventoryRow( row, skillID, _context.Passive.GetTentativeCount( skillID ), unavailable );
                }
                else
                {
                    var skillID = _rightActiveIds[listIndex];
                    _view.SetInventoryRow( row, skillID, _context.Active.GetTentativeCount( skillID ), unavailable );
                }
            }

            _view.SetScrollIndicators( 0 < _rightScrollTop, _rightScrollTop + shownCount < count );
        }

        /// <summary>
        /// 右側で選択中の一覧の行が表示範囲に収まるよう、スクロール位置を調整します。
        /// 「スキルを外す」を選択中の場合は先頭までスクロールします。
        /// </summary>
        /// <returns>スクロール位置が変化したか</returns>
        private bool ScrollToRightSelection()
        {
            int prevTop = _rightScrollTop;
            int listIndex = _rightCmdIdxVal.value - 1;

            if ( listIndex < 0 )
            {
                _rightScrollTop = 0;
            }
            else
            {
                int visibleCount = _view.VisibleInventoryRowCount;
                if ( listIndex < _rightScrollTop )                      { _rightScrollTop = listIndex; }
                else if ( _rightScrollTop + visibleCount <= listIndex ) { _rightScrollTop = listIndex - visibleCount + 1; }
            }

            return prevTop != _rightScrollTop;
        }

        private void RefreshRightSelectionDisplay()
        {
            int value = _rightCmdIdxVal.value;
            _view.SetRightSelectedRow( value == REMOVE_ROW_VALUE ? -1 : value - 1 - _rightScrollTop );
        }

        /// <summary>
        /// フォーカスの移動に応じて変わる表示(スキル説明文、アクションゲージ残量のプレビュー、警告)をまとめて更新します。
        /// </summary>
        private void RefreshFocusDisplay()
        {
            RefreshExplanation();
            RefreshActionGaugeAndWarning();
        }

        /// <summary>
        /// アクションゲージの残量表示と警告表示を更新します。
        /// 右側でパッシブスキル(または「スキルを外す」)にフォーカスしている場合は、確定した場合のゲージの増減を点滅で示し、
        /// 新たに使用出来なくなるアクティブスキルがあればその警告を表示します。アクティブスキルにフォーカスしている場合は、
        /// そのスキルの使用コストが現在のゲージ最大値を超えていれば警告を表示します。
        /// それ以外(左側の操作中等)は、仮の装備構成で既に使用出来ないアクティブスキルがあればその警告を表示します。
        /// </summary>
        private void RefreshActionGaugeAndWarning()
        {
            int baseMax     = _context.Character.GetStatusRef.maxActionGauge;
            int maxNow      = _context.TentativeMaxActionGauge;
            int maxAfter    = maxNow;
            List<ActiveSkillID> becomingUnusable = null;

            if ( TryGetFocusedRightChange( out var slotType, out var slotIndex, out var passiveID, out var activeID ) )
            {
                if ( slotType == SkillSlotType.PASSIVE )
                {
                    maxAfter         = _context.GetMaxActionGaugeAfterPassiveChange( slotIndex, passiveID );
                    becomingUnusable = _context.GetActiveSkillsBecomingUnusableByPassiveChange( slotIndex, passiveID );
                }
                else if ( ActiveSkillsData.IsValidSkill( activeID ) && !_context.IsActiveSkillWithinMaxActionGauge( activeID ) )
                {
                    becomingUnusable = new List<ActiveSkillID> { activeID };
                }
            }

            BuildActionGaugePipStates( baseMax, maxNow, maxAfter );
            _view.SetActionGauge( _pipStates, maxAfter, baseMax );

            if ( becomingUnusable != null && 0 < becomingUnusable.Count )
            {
                _view.ShowWarning( string.Format( _localization.Get( LocKey.UI_SKILL_EQUIP_WARNING_BECOME_UNUSABLE ), JoinSkillNames( becomingUnusable ) ) );
                return;
            }

            var unusable = _context.GetActiveSkillsOverMaxActionGauge();
            if ( 0 < unusable.Count )
            {
                _view.ShowWarning( string.Format( _localization.Get( LocKey.UI_SKILL_EQUIP_WARNING_UNUSABLE ), JoinSkillNames( unusable ) ) );
                return;
            }

            _view.HideWarning();
        }

        /// <summary>
        /// 右側ウィンドウで確定可能な項目にフォーカスしている場合に、確定した場合の変更内容(対象の枠と新しいスキル)を取得します。
        /// 「スキルを外す」の場合、新しいスキルはNONEになります。
        /// </summary>
        private bool TryGetFocusedRightChange( out SkillSlotType slotType, out int slotIndex, out PassiveSkillID passiveID, out ActiveSkillID activeID )
        {
            passiveID = PassiveSkillID.NONE;
            activeID  = ActiveSkillID.NONE;

            if ( !TryGetSlot( _editingRow, out slotType, out slotIndex ) ) return false;
            if ( !CanConfirmRightSelection() ) return false;

            int listIndex = _rightCmdIdxVal.value - 1;
            if ( 0 <= listIndex )
            {
                if ( slotType == SkillSlotType.PASSIVE ) { passiveID = _rightPassiveIds[listIndex]; }
                else                                     { activeID  = _rightActiveIds[listIndex]; }
            }

            return true;
        }

        /// <summary>
        /// アクションゲージ残量表示の目盛りごとの状態を求めます。
        /// 素の最大値までを表示し、変更後も残る分は「残り」、変更で減る分は「消費される(点滅)」、
        /// 変更で戻る分は「戻る(点滅)」、それ以外はパッシブスキルの装備コストとして「消費中」とします。
        /// </summary>
        private void BuildActionGaugePipStates( int baseMax, int maxNow, int maxAfter )
        {
            _pipStates.Clear();
            for ( int i = 0; i < ACTION_GAUGE_MAX; ++i )
            {
                SkillEquipUI.ActionGaugePipState state;
                if ( baseMax <= i )                     { state = SkillEquipUI.ActionGaugePipState.Hidden; }
                else if ( i < maxNow && i < maxAfter )  { state = SkillEquipUI.ActionGaugePipState.Available; }
                else if ( i < maxNow )                  { state = SkillEquipUI.ActionGaugePipState.WillConsume; }
                else if ( i < maxAfter )                { state = SkillEquipUI.ActionGaugePipState.WillRestore; }
                else                                    { state = SkillEquipUI.ActionGaugePipState.Consumed; }

                _pipStates.Add( state );
            }
        }

        private static string JoinSkillNames( List<ActiveSkillID> skillIDs )
        {
            var names = new List<string>( skillIDs.Count );
            foreach ( var skillID in skillIDs )
            {
                names.Add( ActiveSkillsData.data[( int ) skillID].Name.Replace( "_", " " ) );
            }

            return string.Join( ", ", names );
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
                if ( !TryGetSlot( _leftCmdIdxVal.value, out var slotType, out var slotIndex ) ) { _view.HideExplanation(); return; }

                if ( slotType == SkillSlotType.PASSIVE ) { _view.SetExplanation( _context.Passive.GetEquippedSkill( slotIndex ) ); }
                else                                     { _view.SetExplanation( _context.Active.GetEquippedSkill( slotIndex ) ); }
                return;
            }

            if ( _rightCmdIdxVal.value == REMOVE_ROW_VALUE ) { _view.HideExplanation(); return; }

            int listIndex = _rightCmdIdxVal.value - 1;
            if ( listIndex < 0 || RightListCount <= listIndex ) { _view.HideExplanation(); return; }

            if ( _rightListType == SkillSlotType.PASSIVE ) { _view.SetExplanation( _rightPassiveIds[listIndex] ); }
            else                                           { _view.SetExplanation( _rightActiveIds[listIndex] ); }
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
    }
}
