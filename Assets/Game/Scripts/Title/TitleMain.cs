using Frontier.UI;
using System.Collections;

namespace Frontier.Title
{
    /// <summary>
    /// Titleシーンのエントリポイント。FieldMain/GameMainと同様、FocusRoutineControllerを介して
    /// InputFacadeのセットアップ(Awake())と、優先度に基づくルーチン管理(_focusRoutineBhvs)を行う。
    /// Titleシーンは現状Tutorialコンテンツを持たないため、Tutorial初期化を含む共通の
    /// InitCommonRoutine()は使用せず、ルーチン起動とローディング画面解除のみを行う。
    /// </summary>
    public class TitleMain : FocusRoutineController
    {
        protected override int GetRequiredRoutineCount() => (int) FocusRoutinePriority.NUM;

        void Start()
        {
            StartCoroutine( InitGame() );
        }

        void Update()      => base.UpdateRoutine();
        void LateUpdate()  => base.LateUpdateRoutine();
        void FixedUpdate() => base.FixedUpdateRoutine();

        /// <summary>
        /// シーンを初期化します。
        /// </summary>
        private IEnumerator InitGame()
        {
            enabled = false;    // 読込処理完了までUpdate()などを無効にする

            Init();   // ルーチンの起動(MAIN_FLOWであるTitleMenuHandlerのRun()が呼ばれる)

            // フィールド等から遷移してきた場合に暗転したままになっている場合に解除する
            LoadingScreenController.Instance?.Hide();

            enabled = true; // 読込完了したため、Update()などを有効に

            yield break;
        }
    }
}
