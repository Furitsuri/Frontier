using Frontier.Battle;
using Frontier.Entities;
using Frontier.Field;
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

        private StartMode _startMode = StartMode.NEW_GAME;
        private SubRoutineController _current;

        private const string FieldSceneName = "FieldScene";

        public void StartBattle( int stageIndex = 0 )
        {
            var battle = _hierarchyBld.InstantiateWithDiContainer<BattleRoutineController>( true );
            battle.SetStageIndex( stageIndex );
            SwitchTo( battle );
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
                StartBattle();
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
                if( _current is BattleRoutineController && FieldTransitionContext.IsFromField )
                {
                    SceneManager.LoadScene( FieldSceneName );
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