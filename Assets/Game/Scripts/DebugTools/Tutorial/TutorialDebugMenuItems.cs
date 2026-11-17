using Frontier.DebugTools.DebugMenu;
using Frontier.Tutorial;
using Zenject;

#if UNITY_EDITOR

namespace Frontier.DebugTools.Tutorial
{
    /// <summary>
    /// チュートリアルの内容確認・既読データ編集用のデバッグメニューの項目を登録します。
    /// チュートリアルには番号が割り振られていないため、チュートリアルデータの一覧(TutorialDatabase)の先頭から0として扱います
    /// </summary>
    public class TutorialDebugMenuItems
    {
        [Inject] private DebugMenuItemRegistry _registry            = null;
        [Inject] private DebugMenuHandler _menuHandler              = null;
        // チュートリアル機構を持たないシーンでは未バインドのため Optional にする
        [Inject( Optional = true )] private TutorialFacade _tutorialFcd = null;

        private int _selectedIndex = 0;     // 表示するチュートリアルの番号

        /// <summary>
        /// 項目を登録します
        /// </summary>
        public void Register()
        {
            _registry.UnregisterAll( this );    // 再初期化された場合に二重登録しないよう、一度解除する

            if( _tutorialFcd == null ) { return; }

            var page = TutorialDebugMenuPages.ENTRIES;

            // 閲覧と既読データ編集はどちらも番号の指定が共通のため、同じページにまとめる
            _registry.Register( this, new DebugChoiceItem( page, "NUMBER", GetSelectedNumberText, StepSelectedIndex, ShowSelectedTutorial ) );
            _registry.Register( this, new DebugReadOnlyItem( page, "TITLE", GetSelectedTitle ) );
            _registry.Register( this, new DebugToggleItem( page, "READ", IsSelectedShown, SetSelectedShown ) );
            _registry.Register( this, new DebugReadOnlyItem( page, "READ COUNT", GetReadCountText ) );
            _registry.Register( this, new DebugActionItem( page, "SET ALL READ", () => _tutorialFcd.SetAllShown( true ) ) );
            _registry.Register( this, new DebugActionItem( page, "SET ALL UNREAD", () => _tutorialFcd.SetAllShown( false ) ) );
        }

        private int GetTutorialCount()
        {
            return _tutorialFcd.GetTutorialDatas().Count;
        }

        private string GetSelectedNumberText()
        {
            return ( 0 < GetTutorialCount() ) ? _selectedIndex.ToString() : "-";
        }

        private string GetSelectedTitle()
        {
            var datas = _tutorialFcd.GetTutorialDatas();
            return ( _selectedIndex < datas.Count ) ? datas[_selectedIndex].Title : "-";
        }

        /// <summary>
        /// 選択中の番号のチュートリアルが既読かを取得します。番号が無効な場合は未読として扱います
        /// </summary>
        private bool IsSelectedShown()
        {
            var datas = _tutorialFcd.GetTutorialDatas();
            return ( _selectedIndex < datas.Count ) && _tutorialFcd.IsShown( datas[_selectedIndex].TriggerType );
        }

        /// <summary>
        /// 選択中の番号のチュートリアルの既読状態を設定します
        /// </summary>
        private void SetSelectedShown( bool isShown )
        {
            var datas = _tutorialFcd.GetTutorialDatas();
            if( datas.Count <= _selectedIndex ) { return; }

            _tutorialFcd.SetShown( datas[_selectedIndex].TriggerType, isShown );
        }

        /// <summary>
        /// 既読のチュートリアル数を「既読数 / 総数」の形式で取得します
        /// </summary>
        private string GetReadCountText()
        {
            var datas = _tutorialFcd.GetTutorialDatas();
            int readCount = 0;
            foreach( var data in datas )
            {
                if( _tutorialFcd.IsShown( data.TriggerType ) ) { ++readCount; }
            }

            return $"{readCount} / {datas.Count}";
        }

        /// <summary>
        /// 表示するチュートリアルの番号を前後に切り替えます(端に達した場合は反対側の端へ巡回します)
        /// </summary>
        private void StepSelectedIndex( int delta )
        {
            int count = GetTutorialCount();
            if( count <= 0 ) { return; }

            _selectedIndex = ( _selectedIndex + delta + count ) % count;
        }

        /// <summary>
        /// 選択中の番号のチュートリアルを表示します。
        /// チュートリアルはデバッグメニューより優先度が低いため、デバッグメニューを一時的に閉じて表示し、
        /// チュートリアルを閉じた時点でデバッグメニューを元の位置から再開します
        /// </summary>
        private void ShowSelectedTutorial()
        {
            if( _selectedIndex < 0 || GetTutorialCount() <= _selectedIndex ) { return; }

            _menuHandler.CloseTemporarily();
            _tutorialFcd.ShowTutorialAt( _selectedIndex, _menuHandler.ReopenFromTemporaryClose );
        }
    }
}

#endif // UNITY_EDITOR
