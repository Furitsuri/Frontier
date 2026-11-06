using Frontier.Entities;
using Frontier.StateMachine;

namespace Frontier.Battle
{
    public class EmWaitState : UnitPhaseState
    {
        public override void Init( object context )
        {
            base.Init( context );

            // 手番の敵はEmSelectStateから渡される
            // (待機のみの敵にはグリッドカーソルを合わせないため、カーソル位置のキャラクターから求めてはならない)
            var emOwner = context as Enemy;
            NullCheck.AssertNotNull( emOwner, nameof( emOwner ) );

            // 行動をすべて終了
            emOwner?.BattleParams.TmpParam.EndAction();
            // 即終了
            Back();
        }

        // 何もせずに即終了するため、OnActivatedは空実装
        protected override void OnActivated() { }
    }
}