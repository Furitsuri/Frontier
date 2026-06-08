using Frontier.Entities;
using Frontier.StateMachine;

namespace Frontier.Battle
{
    public class EmWaitState : UnitPhaseState
    {
        public override void Init( object context )
        {
            base.Init( context );

            var _emOwner = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter() as Enemy;

            // 行動をすべて終了
            _emOwner.BattleParams.TmpParam.EndAction();
            // 即終了
            Back();
        }

        // 何もせずに即終了するため、OnActivatedは空実装
        protected override void OnActivated() { }
    }
}