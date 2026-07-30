using System;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.SaveLoad
{
    /// <summary>
    /// セーブ/ロード画面の入力受付とライフサイクルを担当するハンドラ。
    /// フィールドに限らず、複数のシーン・呼び出し元から共通して開かれる可能性があるため、
    /// 特定のシーンに紐づかない独立したクラスとしている。
    /// 呼び出し元からShow()で開かれ、閉じた際はコールバックで呼び出し元へ通知する。
    /// 実際のセーブ・ロード処理(ファイルI/O)はまだ実装しない(セーブスロットの概念が未確定のため)。
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

            string titleKey = ( mode == SaveLoadMode.Save ) ? "UI_CMD_SAVE" : "UI_CMD_LOAD";
            _presenter.Show( titleKey );

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
                    if ( !TrySaveToSelectedSlot() )
                    {
                        Debug.Log( "[SaveLoadHandler] オートセーブ枠は手動で保存できません。" );
                    }
                    break;

                case SaveLoadMode.Load:
                    // MEMO: 実際のロード処理(DTOの内容をUserDomain/GameSession等へ反映し、シーン遷移する)は
                    // 未実装。反映すべき要素を洗い出した上で別途実装する。
                    Debug.Log( "[SaveLoadHandler] LOADの実処理は未実装です。" );
                    break;
            }

            return true;
        }

        /// <summary>
        /// 現在選択中のスロットへ、現在のプレイ状況を保存します。
        /// オートセーブ枠(USER_SAVE_AUTO_SLOT_INDEX)は手動保存の対象外のため、保存を行わずfalseを返します。
        /// </summary>
        /// <returns>実際に保存を行った場合はtrue。</returns>
        private bool TrySaveToSelectedSlot()
        {
            int slot = _presenter.GetSelectedSlotIndex();
            if ( slot == USER_SAVE_AUTO_SLOT_INDEX ) return false;

            var data = _userDomain.ToSaveData( GameSession.Instance.FieldProgress );
            _saveHdlr.Save( slot, data );

            _presenter.RefreshSlotContents();

            return true;
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
