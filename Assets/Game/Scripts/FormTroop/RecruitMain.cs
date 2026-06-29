using Frontier.Loaders;
using Frontier.Tutorial;
using Frontier.UI;
using System.Collections;
using UnityEngine;
using Zenject;

namespace Frontier.FormTroop
{
    /// <summary>
    /// RecruitScene のエントリポイント。GameMain の Battle専用処理を取り除いた軽量版。
    /// </summary>
    public class RecruitMain : FocusRoutineController
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;
        [Inject] private InputFacade _inputFcd              = null;
        [Inject] private TutorialFacade _tutorialFcd        = null;

        private InputGuidePresenter _inputGuideView;
        private GeneralFileLoader _generalFileLoader;

        void Awake()
        {
            // 起動シーンに依らずローディング画面の存在を保証する
            LoadingScreenController.EnsureInstance();

            LazyInject.GetOrCreate( ref _inputGuideView, () => _hierarchyBld.InstantiateWithDiContainer<InputGuidePresenter>( false ) );
            LazyInject.GetOrCreate( ref _generalFileLoader, () => _hierarchyBld.InstantiateWithDiContainer<GeneralFileLoader>( true ) );

            // InputFacade はシーンを跨いで永続化されるシングルトン。
            // 入力ガイドUIはこのシーン(RecruitScene)の IUiSystem に紐づくため、シーンに入る度に渡し直す
            _inputFcd.Setup( _inputGuideView );
        }

        void Start()
        {
            _generalFileLoader.LoadSkillsData();

            _inputFcd.Init();

            StartCoroutine( InitGame() );
        }

        void Update()
        {
            base.UpdateRoutine();
        }

        void LateUpdate()
        {
            base.LateUpdateRoutine();
        }

        void FixedUpdate()
        {
            base.FixedUpdateRoutine();
        }

        /// <summary>
        /// シーンを初期化します
        /// </summary>
        private IEnumerator InitGame()
        {
            enabled = false;    // 読込処理完了までUpdate()などを無効にする

            _tutorialFcd.Setup( GetFocusRoutine( FocusRoutinePriority.TUTORIAL ) );

            var tutorialLoadTask = _tutorialFcd.LoadTutorialData();   // チュートリアルデータの読込待ち
            yield return new WaitUntil( () => tutorialLoadTask.IsCompleted );

            _tutorialFcd.Init();

            base.Init();

            // 初期化完了。フィールドシーンから遷移してきた場合の暗転を解除する
            LoadingScreenController.Instance?.Hide();

            // フィールドから遷移してきた場合、遷移開始〜完了(この暗転解除)までの所要時間をログ出力する
            if( Frontier.Field.FieldTransitionContext.TransitionStartTime >= 0.0 )
            {
                double elapsed = Time.realtimeSinceStartupAsDouble - Frontier.Field.FieldTransitionContext.TransitionStartTime;
                Debug.Log( $"[SceneTransition] Field→RecruitScene 遷移所要時間: {elapsed:F3} 秒" );
                Frontier.Field.FieldTransitionContext.ClearTransitionStartTime();
            }

            enabled = true; // 読込完了したため、Update()などを有効に
        }
    }
}
