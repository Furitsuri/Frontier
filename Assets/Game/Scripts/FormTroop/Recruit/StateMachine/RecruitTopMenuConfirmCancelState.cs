
using Frontier.StateMachine;

namespace Frontier.FormTroop
{
    /// <summary>
    /// クッション画面(雇用/解雇選択)からキャンセルされた際、Recruitルーチンから
    /// 脱出してよいかを確認するステート。
    /// 雇用可能キャラクター一覧はRecruitTopMenuStateが保持しているため、YES時は
    /// RequestCancelExit()でRecruitTopMenuStateに払い戻し(雇用予約の取り消し)を要求する。
    /// </summary>
    public sealed class RecruitTopMenuConfirmCancelState : ConfirmPhaseStateBase
    {
        public override void Init( object context )
        {
            base.Init( context );

            ( _confirmPresenter as RecruitPhasePresenter )?.SetConfirmMessage( "cancel recruiting?" );
        }

        protected override bool AcceptConfirm( InputContext context )
        {
            if( !base.AcceptConfirm( context ) ) { return false; }

            if( _commandList.GetCurrentValue() == ( int ) ConfirmTag.YES )
            {
                GetParent<RecruitTopMenuState>()?.RequestCancelExit();
                _isEndedPhase = true;
            }

            Back();

            return true;
        }
    }
}
