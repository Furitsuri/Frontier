using Frontier.StateMachine;

namespace Frontier.Battle
{
    /// <summary>
    /// 攻撃対象が他キャラクターの攻撃予約対象になっており、かつこの攻撃で倒してしまう場合に表示する確認ステートです。
    /// 選択結果は Confirmed で呼び出し元(PlAttackState/PlAttackOnMoveState/PlSkillActionToTargetState)に伝えます。
    /// </summary>
    public sealed class PlConfirmKillReservedTargetState : ConfirmPhaseStateBase
    {
        public bool Confirmed { get; private set; } = false;

        public override void Init( object context )
        {
            base.Init( context );

            Confirmed = false;

            ( _confirmPresenter as BattleRoutinePresenter )?.SetConfirmMessage(
                "この敵は他のキャラクターが攻撃を予約している対象です。\nここで倒すと、予約されたスキルは実行されずに行動終了扱いとなります。よろしいですか？" );
        }

        protected override bool AcceptConfirm( InputContext context )
        {
            if( !base.AcceptConfirm( context ) ) { return false; }

            Confirmed = ( _commandList.GetCurrentValue() == ( int ) ConfirmTag.YES );
            Back();

            return true;
        }
    }
}
