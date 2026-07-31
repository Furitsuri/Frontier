using System;
using UnityEngine;
using Zenject;

namespace Frontier.TroopEdit
{
    /// <summary>
    /// 部隊編集画面の入力受付とライフサイクルを担当するハンドラ。
    /// フィールドに限らず、複数のシーン・呼び出し元から共通して開かれる可能性があるため、
    /// 特定のシーンに紐づかない独立したクラスとしている(SaveLoadHandlerと同様)。
    /// 呼び出し元からShow()で開かれ、閉じた際はコールバックで呼び出し元へ通知する。
    /// </summary>
    public class TroopEditHandler : MonoBehaviour
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;

        private TroopEditPresenter _presenter = null;
        private Action _onClosed = null;
        private int _navHashCode;

        /// <summary>
        /// Presenterを生成します(一度だけ呼び出してください)。
        /// </summary>
        public void Setup()
        {
            _presenter = _hierarchyBld.InstantiateWithDiContainer<TroopEditPresenter>( false );
            _presenter.Init();
        }

        /// <summary>
        /// 部隊編集画面を開き、入力コードの受付を開始します。
        /// </summary>
        /// <param name="onClosed">画面を閉じた際に呼ばれるコールバック(呼び出し元がメニューへ戻る処理を行う)</param>
        public void Show( Action onClosed )
        {
            _onClosed = onClosed;

            _presenter.Show();

            RegisterNavInputCodes();
        }

        /// <summary>
        /// 画面内でのキャンセルに対応する入力コードを登録します。
        /// </summary>
        private void RegisterNavInputCodes()
        {
            _navHashCode = Hash.GetStableHash( nameof( TroopEditHandler ) + "_Nav" );
            InputFacade.Instance.RegisterInputCodes(
                ( GuideIcon.CANCEL, "BACK", InputFacade.CanBeAcceptAlways, new AcceptContextInput( AcceptCancel ), 0.0f, _navHashCode )
            );
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
