using Frontier.Battle;
using Frontier.FormTroop;
using UnityEngine;
using Zenject;

namespace Frontier
{
    public class GameRoutineController : FocusRoutineBase
    {
        [Header( "スキルコントローラオブジェクト" )]
        [SerializeField] private GameObject _skillCtrlObject;

        [Inject] private HierarchyBuilderBase _hierarchyBld = null;

        private SubRoutineController _current;

        public void StartBattle()
        {
            SwitchTo( _hierarchyBld.InstantiateWithDiContainer<BattleRoutineController>( true ) );
        }

        public void StartRecruitment()
        {
            SwitchTo( _hierarchyBld.InstantiateWithDiContainer<FormTroopRoutineController>( false ) );
        }

        private void SwitchTo( SubRoutineController routine )
        {
            _current?.Exit();
            _current = routine;
            _current.Run();
        }

        public override void Init()
        {
            base.Init();

            StartBattle();
        }

        public override void UpdateRoutine()
        {
            _current?.Update();
        }
        public override void LateUpdateRoutine() 
        {
            _current?.LateUpdate();
        }
        public override void FixedUpdateRoutine()
        {
            _current?.FixedUpdate();
        }
        public override int GetPriority() { return ( int ) FocusRoutinePriority.MAIN_FLOW; }
    }
}