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
        private YesNoConfirmPresenter _deleteConfirmPresenter = null;
        private Action _onClosed = null;
        private SaveLoadMode _mode = SaveLoadMode.Save;
        private int _navHashCode;
        private int _deleteConfirmHashCode;

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

            RegisterNavInputCodes();
        }

        /// <summary>
        /// 画面内でのカーソル移動・決定・キャンセル・削除に対応する入力コードを登録します。
        /// 初回オープン時に加え、削除確認ダイアログから復帰した際の再登録にも使用します。
        /// </summary>
        private void RegisterNavInputCodes()
        {
            _navHashCode = Hash.GetStableHash( nameof( SaveLoadHandler ) + "_Nav" );
            InputFacade.Instance.RegisterInputCodes(
                ( GuideIcon.VERTICAL_CURSOR, "SELECT",  InputFacade.CanBeAcceptAlways, new AcceptContextInput( AcceptDirection ), MENU_DIRECTION_INPUT_INTERVAL, _navHashCode ),
                ( GuideIcon.CONFIRM,         "CONFIRM", CanAcceptConfirm,              new AcceptContextInput( AcceptConfirm ),   0.0f, _navHashCode ),
                ( GuideIcon.CANCEL,          "BACK",    InputFacade.CanBeAcceptAlways, new AcceptContextInput( AcceptCancel ),    0.0f, _navHashCode ),
                ( GuideIcon.TOOL,            "DELETE",  CanAcceptDelete,               new AcceptContextInput( AcceptDelete ),    0.0f, _navHashCode )
            );
        }

        private bool AcceptDirection( InputContext context )
        {
            return _presenter.MoveSelection( context.Cursor );
        }

        /// <summary>
        /// CONFIRMの入力受付可否を判定します。SAVE画面では常に受付可能(オートセーブ枠は
        /// SaveLoadPresenter.Show()の時点でカーソル移動の対象から除外済みのため、選択中のスロットは
        /// 常に保存可能)。LOAD画面では選択中のスロットに実際にセーブデータが存在する場合のみ受付可能とする。
        /// </summary>
        private bool CanAcceptConfirm()
        {
            if ( _mode == SaveLoadMode.Save ) return true;

            return _saveHdlr.Exists( _presenter.GetSelectedSlotIndex() );
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
                    LoadSelectedSlot();
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
        /// 保存時のシーンへ遷移します。CanAcceptConfirm()により、データが存在するスロットしか選択できない
        /// ため、ここでの存在チェックは不要。
        /// </summary>
        private void LoadSelectedSlot()
        {
            int slot = _presenter.GetSelectedSlotIndex();
            var data = _saveHdlr.Load( slot );

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

        // ── セーブデータ削除 ─────────────────────────────────────────────────

        /// <summary>
        /// DELETEの入力受付可否を判定します。オートセーブ枠、及びデータが存在しないスロットは
        /// 削除対象外のため受付不可とします。
        /// </summary>
        private bool CanAcceptDelete()
        {
            int slot = _presenter.GetSelectedSlotIndex();

            return slot != USER_SAVE_AUTO_SLOT_INDEX && _saveHdlr.Exists( slot );
        }

        private bool AcceptDelete( InputContext context )
        {
            if ( !context.GetButton( GameButton.Tool ) ) return false;

            OpenDeleteConfirm();

            return true;
        }

        private void EnsureDeleteConfirmPresenter()
        {
            if ( _deleteConfirmPresenter != null ) return;

            _deleteConfirmPresenter = _hierarchyBld.InstantiateWithDiContainer<YesNoConfirmPresenter>( false );
            _deleteConfirmPresenter.Init();
        }

        private void OpenDeleteConfirm()
        {
            EnsureDeleteConfirmPresenter();

            _deleteConfirmPresenter.Show( LocKey.UI_CONFIRM_DELETE_SAVE_MESSAGE );

            InputFacade.Instance.UnregisterInputCodes( _navHashCode );

            _deleteConfirmHashCode = Hash.GetStableHash( nameof( SaveLoadHandler ) + "_DeleteConfirm" );
            InputFacade.Instance.RegisterInputCodes(
                ( GuideIcon.HORIZONTAL_CURSOR, "SELECT",  InputFacade.CanBeAcceptAlways, new AcceptContextInput( AcceptDeleteConfirmDirection ), MENU_DIRECTION_INPUT_INTERVAL, _deleteConfirmHashCode ),
                ( GuideIcon.CONFIRM,           "CONFIRM", InputFacade.CanBeAcceptAlways, new AcceptContextInput( AcceptDeleteConfirmConfirm ),   0.0f, _deleteConfirmHashCode ),
                ( GuideIcon.CANCEL,            "BACK",    InputFacade.CanBeAcceptAlways, new AcceptContextInput( AcceptDeleteConfirmCancel ),    0.0f, _deleteConfirmHashCode )
            );
        }

        private bool AcceptDeleteConfirmDirection( InputContext context )
        {
            return _deleteConfirmPresenter.MoveSelection( context.Cursor );
        }

        private bool AcceptDeleteConfirmConfirm( InputContext context )
        {
            if ( !context.GetButton( GameButton.Confirm ) ) return false;

            if ( _deleteConfirmPresenter.IsYesSelected() )
            {
                int slot = _presenter.GetSelectedSlotIndex();
                _saveHdlr.Delete( slot );
                _presenter.RefreshSlotContents();
            }

            CloseDeleteConfirm();

            return true;
        }

        private bool AcceptDeleteConfirmCancel( InputContext context )
        {
            if ( !context.GetButton( GameButton.Cancel ) ) return false;

            CloseDeleteConfirm();

            return true;
        }

        /// <summary>
        /// 削除確認ダイアログを閉じ、セーブ/ロード画面の入力コードへ復帰します(YES/NO/キャンセル共通の処理)。
        /// </summary>
        private void CloseDeleteConfirm()
        {
            _deleteConfirmPresenter.Hide();
            InputFacade.Instance.UnregisterInputCodes( _deleteConfirmHashCode );

            RegisterNavInputCodes();
        }
    }
}
