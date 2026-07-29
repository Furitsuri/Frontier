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

        private SaveLoadPresenter _presenter = null;
        private Action _onClosed = null;
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
        /// セーブ画面を開き、入力コードの受付を開始します。
        /// </summary>
        /// <param name="onClosed">画面を閉じた際に呼ばれるコールバック(呼び出し元がメニューへ戻る処理を行う)</param>
        public void Show( Action onClosed )
        {
            _onClosed = onClosed;

            // MEMO: ロード画面への切り替えは未実装。差分はタイトル表示のみのため、後日引数化する
            _presenter.Show( "SAVE" );

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

            // MEMO: 現状はSAVE画面からのみ開かれるため、常に保存動作として扱う。
            // ロード画面としての切り替え・ロード確定時の処理は別途実装が必要(タイトル引数と合わせて後日対応)。
            if ( !_presenter.SaveCurrentSelection() )
            {
                Debug.Log( "[SaveLoadHandler] オートセーブ枠は手動で保存できません。" );
            }

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
