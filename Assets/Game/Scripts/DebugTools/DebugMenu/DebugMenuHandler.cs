using System;
using System.Collections.Generic;
using Zenject;
using static Constants;

#if UNITY_EDITOR

namespace Frontier.DebugTools.DebugMenu
{
    /// <summary>
    /// デバッグメニューの操作を処理します。
    /// カテゴリの一覧 → カテゴリ内のページの一覧 → ページ内の項目の一覧、の3階層で構成されます。
    /// 上下で行を選択し、決定で1階層進み、キャンセルで1階層戻ります。
    /// 項目の一覧では、決定・左右の入力を選択中の項目の型に応じた操作として項目へ渡します
    /// </summary>
    public class DebugMenuHandler : BaseHandlerExtendedFocusRoutine
    {
        /// <summary>
        /// 表示中の階層
        /// </summary>
        private enum MenuLevel
        {
            CATEGORY_LIST,  // カテゴリの一覧
            PAGE_LIST,      // カテゴリ内のページの一覧
            ITEM_LIST,      // ページ内の項目の一覧
        }

        private InputFacade _inputFcd = null;
        private DebugMenuItemRegistry _itemRegistry = null;
        private DebugMenuPresenter _debugMenuView = null;
        private int _inputHashCode = -1;
        private MenuLevel _level = MenuLevel.CATEGORY_LIST;
        private List<DebugMenuCategory> _categories = new List<DebugMenuCategory>();
        private List<DebugMenuPage> _pages = new List<DebugMenuPage>();
        private List<DebugMenuItem> _items = new List<DebugMenuItem>();
        // 選択中の行のインデックス
        private int _currentIndex = 0;
        // 上の階層へ戻った際に、直前に開いていた行を選択し直すためのインデックス
        private int _categoryIndex = 0;
        private int _pageIndex = 0;
        // デバッグON/OFFのコールバック
        public delegate void ToggleDebugCallback();
        // デバッグメニューへの遷移の入力可否コールバック
        private InputCode.EnableCallback _canAcceptDebugTransitionCb = null;
        // デバッグメニューへの遷移入力のコールバック
        private AcceptContextInput.AcceptContextInputCallback _acceptDebugTransitionCb = null;

        public ToggleDebugCallback _toggleDebugCb = null;

        [Inject]
        public void Construct( InputFacade inputFcd, DebugMenuItemRegistry itemRegistry )
        {
            _inputFcd       = inputFcd;
            _itemRegistry   = itemRegistry;
        }

        /// <summary>
        /// 初期化します
        /// </summary>
        public void Init( DebugMenuPresenter debugMenuView, ToggleDebugCallback cb, InputCode.EnableCallback canAcceptCb, AcceptContextInput.AcceptContextInputCallback acceptInputCb )
        {
            base.Init();

            _debugMenuView = debugMenuView;
            _toggleDebugCb = cb;
            _inputHashCode = Hash.GetStableHash( GetType().Name );
            _canAcceptDebugTransitionCb = canAcceptCb;
            _acceptDebugTransitionCb = acceptInputCb;

            _categories.Clear();
            for( int i = 0; i < ( int ) DebugMenuCategory.NUM; ++i )
            {
                _categories.Add( ( DebugMenuCategory ) i );
            }
        }

        /// <summary>
        /// デバッグ画面全体とデバッグメニューの表示・非表示を切り替えます
        /// </summary>
        private void ToggleDebugView()
        {
            _toggleDebugCb?.Invoke();
            _debugMenuView.ToggleMenuVisibility();
        }

        private void RegisterInputCodes()
        {
            _inputFcd.RegisterInputCodes(
                (GuideIcon.ALL_CURSOR, "SELECT/VALUE", CanAcceptDirection, new AcceptContextInput( AcceptDirection ), MENU_DIRECTION_INPUT_INTERVAL, _inputHashCode),
                (GuideIcon.CONFIRM, "CONFIRM", CanAcceptConfirm, new AcceptContextInput( AcceptConfirm ), 0.0f, _inputHashCode),
                (GuideIcon.CANCEL, "BACK", CanAcceptCancel, new AcceptContextInput( AcceptCancel ), 0.0f, _inputHashCode)
            );
        }

        /// <summary>
        /// カテゴリの一覧を表示する状態にします
        /// </summary>
        private void OpenCategoryList()
        {
            _level          = MenuLevel.CATEGORY_LIST;
            _currentIndex   = _categoryIndex;
        }

        /// <summary>
        /// 選択中のカテゴリのページの一覧を表示する状態にします
        /// </summary>
        private void OpenPageList()
        {
            _level          = MenuLevel.PAGE_LIST;
            _pages          = _itemRegistry.GetPages( _categories[_categoryIndex] );
            _currentIndex   = Math.Clamp( _pageIndex, 0, Math.Max( 0, _pages.Count - 1 ) );
        }

        /// <summary>
        /// 選択中のページの項目の一覧を表示する状態にします
        /// </summary>
        private void OpenItemList()
        {
            var page = _pages[_pageIndex];
            _itemRegistry.NotifyPageOpened( page );

            _level          = MenuLevel.ITEM_LIST;
            _items          = _itemRegistry.GetItems( page );
            _currentIndex   = 0;
        }

        /// <summary>
        /// 現在の階層の行数を取得します
        /// </summary>
        private int GetRowCount()
        {
            switch( _level )
            {
                case MenuLevel.CATEGORY_LIST:   return _categories.Count;
                case MenuLevel.PAGE_LIST:       return _pages.Count;
                default:                        return _items.Count;
            }
        }

        private bool CanAcceptDirection()
        {
            return 0 < GetRowCount();
        }

        private bool CanAcceptConfirm()
        {
            return 0 < GetRowCount();
        }

        private bool CanAcceptCancel()
        {
            return true;
        }

        /// <summary>
        /// 方向入力を受け取った際の処理を行います。
        /// 上下は行の選択、左右は(項目の一覧でのみ)選択中の項目の値の変更として扱います
        /// </summary>
        private bool AcceptDirection( InputContext context )
        {
            int rowCount = GetRowCount();

            switch( context.Cursor )
            {
                case Direction.FORWARD:
                    _currentIndex = ( _currentIndex - 1 + rowCount ) % rowCount;   // 前の行へ
                    return true;

                case Direction.BACK:
                    _currentIndex = ( _currentIndex + 1 ) % rowCount;              // 次の行へ
                    return true;

                case Direction.LEFT:
                    return _level == MenuLevel.ITEM_LIST && _items[_currentIndex].Step( -1 );

                case Direction.RIGHT:
                    return _level == MenuLevel.ITEM_LIST && _items[_currentIndex].Step( 1 );
            }

            return false;
        }

        /// <summary>
        /// 決定入力を受け取った際の処理を行います。
        /// カテゴリ・ページの一覧では選択中の行を開き、項目の一覧では選択中の項目へ決定操作を渡します
        /// </summary>
        /// <returns>入力実行の有無</returns>
        private bool AcceptConfirm( InputContext context )
        {
            if( !context.GetButton( GameButton.Confirm ) ) { return false; }

            switch( _level )
            {
                case MenuLevel.CATEGORY_LIST:
                    _categoryIndex  = _currentIndex;
                    _pageIndex      = 0;
                    OpenPageList();
                    return true;

                case MenuLevel.PAGE_LIST:
                    _pageIndex = _currentIndex;
                    OpenItemList();
                    return true;

                default:
                    return _items[_currentIndex].Confirm();
            }
        }

        /// <summary>
        /// キャンセル入力を受けた際の処理を行います。
        /// 1階層上へ戻り、カテゴリの一覧ではデバッグメニューを閉じます
        /// </summary>
        /// <returns>入力実行の有無</returns>
        private bool AcceptCancel( InputContext context )
        {
            if( !context.GetButton( GameButton.Cancel ) ) { return false; }

            switch( _level )
            {
                case MenuLevel.ITEM_LIST:
                    OpenPageList();
                    return true;

                case MenuLevel.PAGE_LIST:
                    OpenCategoryList();
                    return true;

                default:
                    ScheduleExit();
                    return true;
            }
        }

        // =========================================================
        // IFocusRoutine 実装
        // =========================================================

        #region IFocusRoutine Implementation

        /// <summary>
        /// 更新を行います
        /// 項目の値はメニュー外の要因でも変化し得るため、毎フレーム表示し直します
        /// </summary>
        public override void UpdateRoutine()
        {
            switch( _level )
            {
                case MenuLevel.CATEGORY_LIST:
                    _debugMenuView.ShowCategoryList( _categories, _currentIndex );
                    break;

                case MenuLevel.PAGE_LIST:
                    _debugMenuView.ShowPageList( _categories[_categoryIndex], _pages, _currentIndex );
                    break;

                default:
                    _debugMenuView.ShowItemPage( _pages[_pageIndex], _items, _currentIndex );
                    break;
            }
        }

        public override void Run()
        {
            base.Run();

            _categoryIndex  = 0;
            _pageIndex      = 0;
            OpenCategoryList();

            ToggleDebugView();
            _inputFcd.UnregisterInputCodes();
            RegisterInputCodes();
        }

        public override void Restart()
        {
            base.Restart();

            _debugMenuView.ToggleMenuVisibility();
            _inputFcd.UnregisterInputCodes( _inputHashCode );
            RegisterInputCodes();
        }

        public override void Pause()
        {
            base.Pause();

            _debugMenuView.ToggleMenuVisibility();
            _inputFcd.UnregisterInputCodes( _inputHashCode );
        }

        public override void Exit()
        {
            base.Exit();

            ToggleDebugView();
            _inputFcd.UnregisterInputCodes( _inputHashCode );
            int hashCode = Hash.GetStableHash( Constants.DEBUG_TRANSION_INPUT_HASH_STRING );
            _inputFcd.RegisterInputCodes( (GuideIcon.DEBUG_MENU, "DEBUG", _canAcceptDebugTransitionCb, new AcceptContextInput( _acceptDebugTransitionCb ), 0.0f, hashCode) );
        }

        public override int GetPriority() { return ( int ) FocusRoutinePriority.DEBUG_MENU; }

        #endregion  // IFocusRoutine 実装
    }
}

#endif // UNITY_EDITOR
