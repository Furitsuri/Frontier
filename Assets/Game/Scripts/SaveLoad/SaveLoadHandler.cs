using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;
using static Constants;

namespace Frontier.SaveLoad
{
    /// <summary>
    /// セーブ/ロード画面の入力受付とライフサイクルを担当するハンドラ。
    /// フィールドに限らず、複数のシーン・呼び出し元から共通して開かれる可能性があるため、
    /// 特定のシーンに紐づかない独立したクラスとしている。
    /// 呼び出し元からShow()で開かれ、閉じた際はコールバックで呼び出し元へ通知する。
    /// </summary>
    public class SaveLoadHandler : MonoBehaviour
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;
        [Inject] private UserDomain _userDomain = null;
        [Inject] private ISlotSaveHandler<UserSaveData> _saveHdlr = null;

        private SaveLoadPresenter _presenter = null;
        private Action _onClosed = null;
        private SaveLoadMode _mode = SaveLoadMode.Save;
        private int _navHashCode;

        /// <summary>
        /// Presenterを生成します(一度だけ呼び出してください)。
        /// </summary>
        public void Setup()
        {
            _presenter = _hierarchyBld.InstantiateWithDiContainer<SaveLoadPresenter>( false );
            _presenter.Init();
        }

        /// <summary>
        /// セーブ/ロード画面を開き、入力コードの受付を開始します。
        /// </summary>
        /// <param name="mode">セーブ画面/ロード画面のどちらとして開くか</param>
        /// <param name="onClosed">画面を閉じた際に呼ばれるコールバック(呼び出し元がメニューへ戻る処理を行う)</param>
        public void Show( SaveLoadMode mode, Action onClosed )
        {
            _mode = mode;
            _onClosed = onClosed;

            _presenter.Show( mode );

            _navHashCode = Hash.GetStableHash( nameof( SaveLoadHandler ) + "_Nav" );
            InputFacade.Instance.RegisterInputCodes(
                ( GuideIcon.VERTICAL_CURSOR, "SELECT",  InputFacade.CanBeAcceptAlways, new AcceptContextInput( AcceptDirection ), MENU_DIRECTION_INPUT_INTERVAL, _navHashCode ),
                ( GuideIcon.CONFIRM,         "CONFIRM", InputFacade.CanBeAcceptAlways, new AcceptContextInput( AcceptConfirm ),   0.0f, _navHashCode ),
                ( GuideIcon.CANCEL,          "BACK",    InputFacade.CanBeAcceptAlways, new AcceptContextInput( AcceptCancel ),    0.0f, _navHashCode )
            );
        }

        private bool AcceptDirection( InputContext context )
        {
            return _presenter.MoveSelection( context.Cursor );
        }

        private bool AcceptConfirm( InputContext context )
        {
            if ( !context.GetButton( GameButton.Confirm ) ) return false;

            switch ( _mode )
            {
                case SaveLoadMode.Save:
                    SaveToSelectedSlot();
                    break;

                case SaveLoadMode.Load:
                    TryLoadSelectedSlot();
                    break;
            }

            return true;
        }

        /// <summary>
        /// 現在選択中のスロットへ、現在のプレイ状況を保存します。
        /// オートセーブ枠(USER_SAVE_AUTO_SLOT_INDEX)はSaveLoadPresenter.Show()の時点でカーソル移動の
        /// 対象から除外されているため、ここで選択されることはない。
        /// </summary>
        private void SaveToSelectedSlot()
        {
            int slot = _presenter.GetSelectedSlotIndex();

            var data = _userDomain.ToSaveData( GameSession.Instance.FieldProgress );
            data.SceneName = SceneManager.GetActiveScene().name;
            _saveHdlr.Save( slot, data );

            _presenter.RefreshSlotContents();
        }

        /// <summary>
        /// 現在選択中のスロットのセーブデータを読み込み、UserDomain/GameSession.FieldProgressへ反映した上で、
        /// 保存時のシーンへ遷移します。データが存在しない場合は何も行いません。
        /// </summary>
        private void TryLoadSelectedSlot()
        {
            int slot = _presenter.GetSelectedSlotIndex();
            var data = _saveHdlr.Load( slot );
            if ( data == null )
            {
                Debug.Log( "[SaveLoadHandler] このスロットにはセーブデータがありません。" );
                return;
            }

            _userDomain.ApplySaveData( data );
            GameSession.Instance.FieldProgress = data.FieldProgress;
            GameSession.Instance.IsResumedFromSave = true;

            SceneManager.LoadScene( data.SceneName );
        }

        private bool AcceptCancel( InputContext context )
        {
            if ( !context.GetButton( GameButton.Cancel ) ) return false;

            Close();

            return true;
        }

        private void Close()
        {
            _presenter.Hide();

            InputFacade.Instance.UnregisterInputCodes( _navHashCode );

            // UnregisterInputCodesだけでは入力ガイド表示が更新されないため、空登録でガイドの再描画を促す
            InputFacade.Instance.RegisterInputCodes();

            var callback = _onClosed;
            _onClosed = null;
            callback?.Invoke();
        }
    }
}
