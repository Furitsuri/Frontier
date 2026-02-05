using Frontier.Battle;
using Frontier.Entities;
using Frontier.FormTroop;
using UnityEngine;
using Zenject;
using static Frontier.BattleFileLoader;

namespace Frontier
{
    public class GameRoutineController : FocusRoutineBase
    {
        [Header( "スキルコントローラオブジェクト" )]
        [SerializeField] private GameObject _skillCtrlObject;

        [Inject] private HierarchyBuilderBase _hierarchyBld         = null;
        [Inject] private UserDomain _userDomain                     = null;

        private StartMode _startMode = StartMode.NEW_GAME;
        private SubRoutineController _current;

        public void StartBattle()
        {
            SwitchTo( _hierarchyBld.InstantiateWithDiContainer<BattleRoutineController>( true ) );
        }

        public void StartRecruit()
        {
            // TODO : 仮実装
            _userDomain.AddMoney( 1000 );

            SwitchTo( _hierarchyBld.InstantiateWithDiContainer<FormTroopRoutineController>( false ) );
        }

        private void SwitchTo( SubRoutineController routine )
        {
            _current?.Exit();
            _current = routine;
            _current.Setup();
            _current.Run();
        }

        public override void Init()
        {
            base.Init();

            if( _startMode == StartMode.NEW_GAME )
            {
                StartNewGame();
            }

            StartRecruit();
            // StartBattle();
        }

        public override void UpdateRoutine()
        {
            _current.Update();
        }
        public override void LateUpdateRoutine() 
        {
            if( _current.LateUpdate() )
            {
                if( _current is BattleRoutineController )
                {
                    StartRecruit();
                }
                else if( _current is FormTroopRoutineController )
                {
                    StartBattle();
                }
            }
        }
        public override void FixedUpdateRoutine()
        {
            _current.FixedUpdate();
        }
        public override int GetPriority() { return ( int ) FocusRoutinePriority.MAIN_FLOW; }

        private void StartNewGame()
        {
        }
    }
}