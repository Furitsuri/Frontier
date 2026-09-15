using Frontier.StateMachine;
using Frontier.TroopEdit;
using Frontier.UI;
using Zenject;

namespace Frontier.FormTroop
{
    /// <summary>
    /// 解雇チェック済みメンバーをまとめて解雇してよいかを確認するステート(RecruitDismissStateの子)。
    /// 絞り込み後のキャラクター一覧を十字キーで選択・INFOでステータス確認できる
    /// (RecruitGridConfirmStateBase参照)。
    /// </summary>
    public sealed class RecruitDismissConfirmCompletedState : RecruitGridConfirmStateBase
    {
        [Inject] private TalkWindowConfirmPresenter _talkConfirmPresenter = null;

        public override void Init( object context )
        {
            base.Init( context );

            int checkedCount = 0;
            ReceiveContext( ref checkedCount, context );

            _talkConfirmPresenter.SetConfirmMessage( LocKey.UI_TALK_SHOPKEEPER_NAME,
                checkedCount == 1 ? LocKey.UI_TALK_DISMISS_CONFIRM_SINGULAR : LocKey.UI_TALK_DISMISS_CONFIRM_PLURAL );
        }

        /// <summary>
        /// 解雇完了確認は会話ウィンドウ形式(TalkWindowConfirmPresenter)を使うため、
        /// ツリー全体に伝播される既定のRecruitPhasePresenterではなく専用のPresenterを割り当てる。
        /// </summary>
        public override void AssignPresenter( PhasePresenterBase presenter )
        {
            _confirmPresenter = _talkConfirmPresenter;
        }

        /// <summary>
        /// 親(RecruitDismissState)が保持するTroopGridControllerを、この確認画面でも使い回します。
        /// </summary>
        protected override TroopGridController GetGridController()
        {
            return GetParent<RecruitDismissState>()?.GridController;
        }

        protected override bool AcceptConfirm( InputContext context )
        {
            if( !base.AcceptConfirm( context ) ) { return false; }

            if( _commandList.GetCurrentValue() == ( int ) ConfirmTag.YES )
            {
                // 解雇チェック済みメンバーを自軍から取り除く(RecruitSceneは終了しない)
                GetParent<RecruitDismissState>()?.CommitDismissal();
            }

            Back();

            return true;
        }
    }
}
