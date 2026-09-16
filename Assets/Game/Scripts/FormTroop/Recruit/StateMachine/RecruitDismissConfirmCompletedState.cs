using Frontier.StateMachine;
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

            _talkConfirmPresenter.SetConfirmMessage( LocKey.UI_TALK_SHOPKEEPER_NAME,
                ToggledCount == 1 ? LocKey.UI_TALK_DISMISS_CONFIRM_SINGULAR : LocKey.UI_TALK_DISMISS_CONFIRM_PLURAL );
        }

        /// <summary>
        /// 解雇完了確認は会話ウィンドウ形式(TalkWindowConfirmPresenter)を使うため、
        /// ツリー全体に伝播される既定のRecruitPhasePresenterではなく専用のPresenterを割り当てる。
        /// </summary>
        public override void AssignPresenter( PhasePresenterBase presenter )
        {
            _confirmPresenter = _talkConfirmPresenter;
        }

        protected override bool AcceptConfirm( InputContext context )
        {
            if( !base.AcceptConfirm( context ) ) { return false; }

            if( _commandList.GetCurrentValue() == ( int ) ConfirmTag.YES )
            {
                // 解雇チェック済みメンバーを自軍から取り除く(RecruitSceneは終了しない)
                CommitConfirmedSelection();
            }

            Back();

            return true;
        }
    }
}
