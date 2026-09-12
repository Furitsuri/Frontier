
using Frontier.StateMachine;
using Frontier.UI;
using Zenject;

namespace Frontier.FormTroop
{
    public class RecruitConfirmCompletedState : ConfirmPhaseStateBase
    {
        [Inject] private TalkWindowConfirmPresenter _talkConfirmPresenter = null;

        public override void Init( object context )
        {
            base.Init( context );

            int employedCount = 0;
            ReceiveContext( ref employedCount, context );

            _talkConfirmPresenter.SetConfirmMessage( LocKey.UI_TALK_SHOPKEEPER_NAME,
                employedCount == 1 ? LocKey.UI_TALK_EMPLOY_CONFIRM_SINGULAR : LocKey.UI_TALK_EMPLOY_CONFIRM_PLURAL );
        }

        /// <summary>
        /// 雇用完了確認は会話ウィンドウ形式(TalkWindowConfirmPresenter)を使うため、
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
                // 雇用確定キャラクターを自軍へ加え、表示から取り除く(RecruitSceneは終了しない)
                GetParent<RecruitRootState>()?.CommitEmployment();
            }

            Back();

            return true;
        }
    }
}