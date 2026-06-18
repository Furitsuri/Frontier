using Frontier.Battle;
using Frontier.Entities;
using Frontier.Field;
using Frontier.FormTroop;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;
using static Frontier.Loaders.BattleFileLoader;

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

        private const string FieldSceneName = "FieldScene";

        public void StartBattle( int stageIndex = 0 )
        {
            var battle = _hierarchyBld.InstantiateWithDiContainer<BattleRoutineController>( true );
            battle.SetStageIndex( stageIndex );
            SwitchTo( battle );
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

            if ( FieldTransitionContext.IsFromField )
            {
                StartBattle( FieldTransitionContext.StageIndex );
            }
            else
            {
                StartRecruit();
                // StartBattle();
            }
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
                    if ( FieldTransitionContext.IsFromField )
                    {
                        SceneManager.LoadScene( FieldSceneName );
                    }
                    else
                    {
                        StartRecruit();
                    }
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

        public override void Run()
        {
            base.Run();
        }

        public override void Restart()
        {
            base.Restart();

            _current.Restart();
        }

        public override void Pause()
        {
            _current.Pause();

            base.Pause();
        }

        public override void Exit()
        {
            _current.Exit();

            base.Exit();
        }

        private void StartNewGame()
        {
        }
    }
}