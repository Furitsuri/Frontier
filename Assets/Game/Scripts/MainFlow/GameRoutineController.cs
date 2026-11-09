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

        [Header( "フィールドを経由せずに直接起動した際(開発用の起動)に読み込むステージ名(FilePathRegistryのステージ名。空の場合は先頭のステージ)" )]
        [SerializeField] private string _directLaunchStageName = string.Empty;

        [Inject] private HierarchyBuilderBase _hierarchyBld         = null;
        [Inject] private Frontier.Registries.FilePathRegistry _filePathReg = null;

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
                StartBattle( ResolveDirectLaunchStageIndex() );
            }
        }

        /// <summary>
        /// フィールドを経由せずに直接起動した際に読み込むステージのインデックスを求めます。
        /// ステージ名が未指定、または見つからない場合は先頭のステージとします
        /// </summary>
        private int ResolveDirectLaunchStageIndex()
        {
            if( string.IsNullOrEmpty( _directLaunchStageName ) ) { return 0; }

            int index = System.Array.IndexOf( _filePathReg.StageNames, _directLaunchStageName );
            if( index < 0 )
            {
                Debug.LogWarning( $"[GameRoutineController] 直接起動時のステージ名「{_directLaunchStageName}」がFilePathRegistryに見つかりません。先頭のステージを読み込みます。" );
                return 0;
            }

            return index;
        }

        public override void UpdateRoutine()
        {
            _current.Update();
        }

        public override void UpdatePresentation()
        {
            _current?.UpdatePresentation();
        }

        public override void LateUpdateRoutine()
        {
            if( _current.LateUpdate() )
            {
                if( _current is BattleRoutineController )
                {
                    if( FieldTransitionContext.IsFromField )
                    {
                        SceneManager.LoadScene( FieldSceneName );
                    }
                    else
                    {
                        // Fieldを経由せずBattleSceneが直接起動された場合(開発者によるデバッグ起動)は、
                        // 遷移先が存在しないためPlayを終了する
#if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false;
#endif
                    }
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