
using Frontier.StateMachine;

namespace Frontier.FormTroop
{
    public class RecruitConfirmCompletedState : ConfirmPhaseStateBase
    {
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